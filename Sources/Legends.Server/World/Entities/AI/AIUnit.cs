﻿using Legends.Core;
using Legends.Core.DesignPattern;
using Legends.Core.Geometry;
using Legends.Core.Protocol;
using Legends.Core.Utils;
using Legends.Protocol.GameClient.Messages.Game;
using Legends.Protocol.GameClient.Types;
using Legends.Records;
using Legends.World.Entities.AI.BasicAttack;
using Legends.World.Entities.Buildings;
using Legends.World.Entities.Loot;
using Legends.World.Entities.Movements;
using Legends.World.Entities.Statistics;
using Legends.World.Entities.Statistics.Replication;
using Legends.World.Items;
using Legends.World.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Legends.World.Entities.AI
{
    public abstract class AIUnit : AttackableUnit
    {
        public const float LOCAL_RANGE = 1000f;

        static Logger logger = new Logger();

        public PathManager PathManager
        {
            get;
            private set;
        }
        public AttackManager AttackManager
        {
            get;
            private set;
        }
        public AIUnitRecord Record
        {
            get;
            private set;
        }
        public SpellManager SpellManager
        {
            get;
            private set;
        }
        /// <summary>
        /// Gère les modèles etc
        /// </summary>
        private Dictionary<uint, CharacterStack> CharacterStacks
        {
            get;
            set;
        }
        public float AttackRange
        {
            get
            {
                return Stats.AttackRange.TotalSafe;
            }
        }
        public override bool IsMoving => PathManager.IsMoving;

        public override float SelectionRadius => (float)Record.SelectionRadius;

        public abstract void OnSpellUpgraded(byte spellId, Spell targetSpell);

        public override float PathfindingCollisionRadius => (float)Record.PathfindingCollisionRadius;

        public abstract bool DefaultAutoattackActivated
        {
            get;
        }
        public virtual void OnTargetSet(AttackableUnit target)
        {

        }
        public virtual void OnTargetUnset(AttackableUnit target)
        {

        }

        public AIUnit(uint netId, AIUnitRecord record) : base(netId)
        {
            this.Record = record;
            this.CharacterStacks = new Dictionary<uint, CharacterStack>();
        }

        public override void Initialize()
        {
            if (Record.IsMelee)
            {
                AttackManager = new MeleeManager(this);
            }
            else
            {
                AttackManager = new RangedManager(this);
            }
            AttackManager.SetAutoattackActivated(DefaultAutoattackActivated);
            SpellManager = new SpellManager(this);
            PathManager = new PathManager(this);
            base.Initialize();
        }
        [InDevelopment(InDevelopmentState.TODO, "Gérer ça correctement lorsque je passerais sur les spells :3")]
        public virtual void AddStackData(string newModel, uint skinId, bool modelOnly, bool overrideSpells,
            bool replaceCharacterPackage, bool notif = true)
        {
            CharacterStacks.Clear();

            AIUnitRecord record = AIUnitRecord.GetAIUnitRecord(newModel);

            if (record != null)
            {
                CharacterStacks.Clear();
                uint id = 1;
                var characterStack = new CharacterStack(record, id, modelOnly, overrideSpells, replaceCharacterPackage, skinId);
                CharacterStacks.Add(id, characterStack);

                if (notif)
                    Game.Send(new ChangeCharacterDataMessage(NetId, characterStack.GetProtocolObject()));
            }
            else
            {
                logger.Write("Unable to assign skin, (" + newModel + ") is not a valid AIUnit model name.",
                    MessageState.WARNING);
            }
        }
        public CharacterStackData[] GetCharacterStackDatas()
        {
            return Array.ConvertAll(CharacterStacks.Values.ToArray(), x => x.GetProtocolObject());
        }

        public abstract void Create();

        public void AddGlobalShield(float value)
        {
            Shields.MagicalAndPhysical += value;
            OnShieldModified(true, true, value);
        }

        [InDevelopment(InDevelopmentState.TODO)]
        protected override void ApplyExperienceLoot(AttackableUnit source)
        {

        }
        protected override void ApplyGoldLoot(AttackableUnit source)
        {
            float goldFromLastHit = LootManager.Instance.GetGoldLoot(this);

            if (goldFromLastHit != 0)
            {
                source.AddGold(goldFromLastHit, true);
            }

            float goldGlobal = Record.GlobalGoldGivenOnDeath;

            if (goldGlobal != 0)
            {
                source.Team.Iteration<AIUnit>(x => x.AddGold(goldGlobal, true));
            }

            float goldLocal = Record.LocalGoldGivenOnDeath;

            if (goldLocal != 0)
            {
                var units = source.Team.GetUnits<AIUnit>(x => x.GetDistanceTo(this) < LOCAL_RANGE);

                foreach (var unit in units)
                {
                    unit.AddGold(goldLocal, true);
                }
            }
        }
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            SpellManager.Update(deltaTime);
            PathManager.Update(deltaTime);
            this.AttackManager.Update(deltaTime);
        }
        public void Move(List<Vector2> waypoints, bool unsetTarget = true, bool notify = true)
        {
            if (SpellManager.IsChanneling())
            {
                return;
            }
            if (Stats.MoveSpeed.TotalSafe > 0)
            {
                if (unsetTarget)
                {
                    AttackManager.StopAttackTarget();

                    if (AttackManager.IsAttacking && !AttackManager.CurrentAutoattack.Casted)
                    {
                        AttackManager.DestroyAutoattack();
                    }
                }

                PathManager.Move(waypoints);

                if (notify)
                    OnMove();
            }

        }
        public override void OnItemAdded(Item item)
        {
            Game.Send(new BuyItemAnswerMessage(NetId, (int)item.Id, item.Slot, item.Stacks, 0x29));
        }
        public override void OnItemRemoved(Item item)
        {
            Game.Send(new InventoryRemoveItemMessage(NetId, item.Slot, 0));
        }
        public T GetAttackManager<T>() where T : AttackManager
        {
            return (T)AttackManager;
        }
        public void Teleport(Vector2 position)
        {
            position = Game.Map.Record.GetClosestTerrainExit(position);
            Position = position;
            PathManager.Move(new List<Vector2>() { Position });
            OnTeleport();
        }
        public virtual void OnMove()
        {
            SendVision(new WaypointListMessage(NetId, Environment.TickCount, PathManager.GetWaypoints()));
        }
        public virtual void OnTeleport()
        {
            SendVision(new WaypointGroupMessage(NetId, Environment.TickCount, new List<MovementDataNormal>() { (MovementDataNormal)GetMovementData() }), Channel.CHL_LOW_PRIORITY);
        }

        public void MoveTo(Vector2 targetVector, bool unsetTarget = true)
        {
            Move(new List<Vector2>() { Position, targetVector }, unsetTarget);
        }
        public void StopMove(bool unsetTarget = true, bool notify = true)
        {
            Move(new List<Vector2>() { Position }, unsetTarget, notify);
        }
        [InDevelopment(InDevelopmentState.THINK_ABOUT_IT, "Not sure about values...check again in RAF?")]
        public virtual float GetAutoattackRange(AttackableUnit target)
        {
            return Stats.AttackRange.TotalSafe + ((float)target.SelectionRadius * target.Stats.ModelSize.TotalSafe);
        }
        [InDevelopment(InDevelopmentState.THINK_ABOUT_IT, "Not sure about values...check again in RAF?")]
        public virtual float GetAutoattackRangeWhileChasing(AttackableUnit target)
        {
            return Stats.AttackRange.TotalSafe + ((float)target.PathfindingCollisionRadius * target.Stats.ModelSize.TotalSafe);
            return Stats.AttackRange.TotalSafe + ((float)target.SelectionRadius * target.Stats.ModelSize.TotalSafe);
        }
        [InDevelopment(InDevelopmentState.TODO, "We need to use pathfinding only for melee to join target.")]
        /// <summary>
        /// On essaye d'auto attack une cible, Si elle est a portée on lance l'animation
        /// Sinon, on marche jusqu'a elle
        /// </summary>
        /// <param name="targetUnit"></param>
        public void TryBasicAttack(AttackableUnit targetUnit)
        {
            if (!targetUnit.Alive)
            {
                return;
            }
            if (this.GetDistanceTo(targetUnit) <= GetAutoattackRange(targetUnit))
            {
                if (IsMoving) // Si on est en mouvement, on s'arrête
                {
                    StopMove();
                }
                AttackManager.BeginAttackTarget(targetUnit); // on lance la première auto
            }
            else
            {
                if (Stats.MoveSpeed.TotalSafe > 0)
                {
                    AttackManager.StopAttackTarget(); // on arrête d'attaquer l'éventuelle cible, car on va se déplacer.

                    if (AttackManager.IsAttacking && !AttackManager.CurrentAutoattack.Casted) // Si on a cancel l'auto avant que les dégats soit infligés, alors on peut la disposer.
                    {
                        AttackManager.DestroyAutoattack();
                    }

                    Action onTargetReach = new Action(() => { TryBasicAttack(targetUnit); }); // recursive call 
                    PathManager.MoveToTarget(targetUnit, onTargetReach, GetAutoattackRangeWhileChasing(targetUnit));
                    OnMove();
                }
            }

        }

        public void CastSpell(byte spellSlot, Vector2 position, Vector2 endPosition, AttackableUnit target, AttackableUnit autoAttackTarget = null)
        {
            Spell spell = SpellManager.GetSpell(spellSlot);

            if (SpellManager.IsChanneling() && !spell.IsSummonerSpell)
            {
                return;
            }
            spell.Cast(position, endPosition, target, autoAttackTarget);
            Game.Send(new CastSpellAnswerMessage(NetId, Environment.TickCount, false, spell.GetCastInformations(
                new Vector3(position.X, position.Y, Game.Map.Record.GetZ(position) + 100),
                new Vector3(endPosition.X, endPosition.Y, Game.Map.Record.GetZ(endPosition) + 100),
                spell.Record.Name)));
        }
        public virtual int GetHash()
        {
            return (int)Record.Name.HashString();
        }
        short test = 0;
        public virtual MovementData GetMovementData()
        {
            if (!IsMoving)
            {
                return new MovementDataNormal()
                {
                    HasTeleportID = true,
                    TeleportID = (byte)test++,
                    TeleportNetID = NetId,
                    Waypoints = PathManager.GetWaypointsTranslated(),
                };
            }
            else
            {
                return new MovementDataNormal()
                {
                    HasTeleportID = false,
                    TeleportID = 0,
                    TeleportNetID = NetId,
                    Waypoints = PathManager.GetWaypointsTranslated(),
                };
            }
        }

    }
}
