﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace xSaliceReligionAIO
{
    class Champion
    {
        protected Champion()
        {
            //Events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnGameSendPacket += Game_OnSendPacket;
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            GameObject.OnDelete += GameObject_OnDelete;
            Obj_AI_Base.OnIssueOrder += ObjAiHeroOnOnIssueOrder;
            Spellbook.OnUpdateChargedSpell += Spellbook_OnUpdateChargedSpell;

            if (menu.Item("Orbwalker_Mode", true).GetValue<bool>())
            {
                Orbwalking.AfterAttack += AfterAttack;
                Orbwalking.BeforeAttack += BeforeAttack;
            }
            else
            {
                xSLxOrbwalker.AfterAttack += AfterAttack;
                xSLxOrbwalker.BeforeAttack += BeforeAttack;
            }

        }

        public Champion(bool load)
        {
            if(load)
                GameOnLoad();
        }

        //Orbwalker instance
        private Orbwalking.Orbwalker _orbwalker;

        //Player instance
        protected readonly Obj_AI_Hero Player = ObjectManager.Player;

        //Spells
        protected readonly List<Spell> SpellList = new List<Spell>();

        protected Spell P;
        protected Spell Q;
        protected Spell Q2;
        protected Spell QExtend;
        protected Spell W;
        protected Spell W2;
        protected Spell E;
        protected Spell E2;
        protected Spell R;
        protected Spell R2;
        protected readonly SpellDataInst QSpell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
        protected readonly SpellDataInst ESpell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
        protected readonly SpellDataInst WSpell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
        protected readonly SpellDataInst RSpell = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R);

        //summoners
        private readonly SpellSlot _igniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
        public readonly SpellSlot _flashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
        //items
        protected readonly Items.Item Dfg = Utility.Map.GetMap().Type == Utility.Map.MapType.TwistedTreeline ? new Items.Item(3188, 750) : new Items.Item(3128, 750);
        protected int LastPlaced;
        protected Vector3 LastWardPos;

        //Mana Manager
        protected int[] QMana = { 0, 0, 0, 0, 0, 0 };
        protected int[] WMana = { 0, 0, 0, 0, 0, 0 };
        protected int[] EMana = { 0, 0, 0, 0, 0, 0 };
        protected int[] RMana = { 0, 0, 0, 0, 0, 0 };

        //Menu
        protected static Menu menu;
        private static readonly Menu OrbwalkerMenu = new Menu("走 砍", "Orbwalker");

        private void GameOnLoad()
        {
            Game.PrintChat("<font color = \"#FFB6C1\">鑺辫竟姹夊寲-xSalice瀹楁暀鍚堥泦</font> - <font color = \"#00FFFF\">鍔犺浇鎴愬姛!</font>");
            Game.PrintChat("<font color = \"#87CEEB\">Feel free to donate via Paypal to:</font> <font color = \"#FFFF00\">xSalicez@gmail.com</font>");

            menu = new Menu(Player.ChampionName, Player.ChampionName, true);

            //Info
            menu.AddSubMenu(new Menu("信 息", "Info"));
            menu.SubMenu("Info").AddItem(new MenuItem("Author", "作者: xSalice"));
            menu.SubMenu("Info").AddItem(new MenuItem("Paypal", "捐赠: xSalicez@gmail.com"));
            menu.SubMenu("Info").AddItem(new MenuItem("Huabian", "汉化:花边下丶情未央"));
            menu.SubMenu("Info").AddItem(new MenuItem("Hanhua", "宗教完美汉化"));
            menu.SubMenu("Info").AddItem(new MenuItem("ErgouQQqun", "QQ群:361630847"));

            //Target selector
            var targetSelectorMenu = new Menu("目标 选择", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            menu.AddSubMenu(targetSelectorMenu);

            //Orbwalker submenu
            OrbwalkerMenu.AddItem(new MenuItem("Orbwalker_Mode", "选择 走砍模式", true).SetValue(false));
            menu.AddSubMenu(OrbwalkerMenu);
            ChooseOrbwalker(menu.Item("Orbwalker_Mode", true).GetValue<bool>());

            //Packet Menu
            menu.AddSubMenu(new Menu("封包 设置", "Packets"));
            menu.SubMenu("Packets").AddItem(new MenuItem("packet", "使用 封包", true).SetValue(false));

            //Item Menu
            var itemMenu = new Menu("物品 and 召唤师技能", "Items");
            ActiveItems.AddToMenu(itemMenu);
            menu.AddSubMenu(itemMenu);

            menu.AddToMainMenu();

            try
            {
                if (Activator.CreateInstance(null, "xSaliceReligionAIO.Champions." + Player.ChampionName) != null)
                {
                    Game.PrintChat("<font color = \"#FFB6C1\">xSalice's 瀹楁暀鍚堥泦" + Player.ChampionName + " 鍔犺浇鎴愬姛!</font>");
                }
            }
            catch
            {
                Game.PrintChat("鑺辫竟鎻愮ず:姝よ嫳闆勪笉鍦ㄥ畻鏁欏悎闆嗘敮鎸佺殑鍚嶅崟涔嬪唴", Player.ChampionName);
            }
        }

        private void ChooseOrbwalker(bool mode)
        {
            if (Player.ChampionName == "Azir")
            {
                xSLxOrbwalker.AddToMenu(OrbwalkerMenu);
                Game.PrintChat("xSLx 璧扮爫 鍔犺浇鎴愬姛!");
                return;
            }

            if (mode)
            {
                _orbwalker = new Orbwalking.Orbwalker(OrbwalkerMenu);
                Game.PrintChat("Regular 璧扮爫 鍔犺浇鎴愬姛!");
            }
            else
            {
                xSLxOrbwalker.AddToMenu(OrbwalkerMenu);
                Game.PrintChat("xSLx 璧扮爫 鍔犺浇鎴愬姛!");
            }
        }
        protected bool packets()
        {
            return menu.Item("packet", true).GetValue<bool>();
        }

        protected void Use_DFG(Obj_AI_Hero target)
        {
            if (target != null && Player.Distance(target) < 750 && Items.CanUseItem(Dfg.Id))
                Items.UseItem(Dfg.Id, target);
        }

        protected bool Ignite_Ready()
        {
            return _igniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready;
        }

        protected bool Flash_Ready()
        {
            return _flashSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(_flashSlot) == SpellState.Ready;
        }

        protected float GetHealthPercent(Obj_AI_Hero unit = null)
        {
            if (unit == null)
                unit = Player;
            return (unit.Health / unit.MaxHealth) * 100f;
        }
        protected bool HasBuff(Obj_AI_Base target, string buffName)
        {
            return target.Buffs.Any(buff => buff.Name == buffName);
        }

        private bool IsWall(Vector2 pos)
        {
            return (NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Wall ||
                    NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Building);
        }
        protected bool IsPassWall(Vector3 start, Vector3 end)
        {
            double count = Vector3.Distance(start, end);
            for (uint i = 0; i <= count; i += 25)
            {
                Vector2 pos = start.To2D().Extend(Player.ServerPosition.To2D(), -i);
                if (IsWall(pos))
                    return true;
            }
            return false;
        }
        protected int countEnemiesNearPosition(Vector3 pos, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>().Count(
                    hero => hero.IsEnemy && !hero.IsDead && hero.IsValid && hero.Distance(pos) <= range);
        }

        protected int countAlliesNearPosition(Vector3 pos, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>().Count(
                    hero => hero.IsAlly && !hero.IsDead && hero.IsValid && hero.Distance(pos) <= range);
        }

        protected bool ManaCheck()
        {
            int totalMana = QMana[Q.Level] + WMana[W.Level] + EMana[E.Level] + RMana[R.Level];
            var checkMana = menu.Item("mana", true).GetValue<bool>();

            if (Player.Mana >= totalMana || !checkMana)
                return true;

            return false;
        }

        protected bool ManaCheck2()
        {
            int totalMana = QMana[Q.Level] + WMana[W.Level] + EMana[E.Level] + RMana[R.Level];

            if (Player.Mana >= totalMana)
                return true;

            return false;
        }

        protected bool IsStunned(Obj_AI_Base target)
        {
            if (target.HasBuffOfType(BuffType.Stun) || target.HasBuffOfType(BuffType.Snare) ||
                target.HasBuffOfType(BuffType.Suppression) || target.HasBuffOfType(BuffType.Taunt))
                return true;

            return false;
        }
        protected bool IsRecalling()
        {
            return Player.HasBuff("Recall");
        }

        protected PredictionOutput GetP(Vector3 pos, Spell spell, Obj_AI_Base target, float delay, bool aoe)
        {
            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay + delay,
                Radius = spell.Width,
                Speed = spell.Speed,
                From = pos,
                Range = spell.Range,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = Player.ServerPosition,
                Aoe = aoe,
            });
        }

        protected PredictionOutput GetP(Vector3 pos, Spell spell, Obj_AI_Base target, bool aoe)
        {
            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay,
                Radius = spell.Width,
                Speed = spell.Speed,
                From = pos,
                Range = spell.Range,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = Player.ServerPosition,
                Aoe = aoe,
            });
        }
        protected PredictionOutput GetPCircle(Vector3 pos, Spell spell, Obj_AI_Base target, bool aoe)
        {
            return Prediction.GetPrediction(new PredictionInput
            {
                Unit = target,
                Delay = spell.Delay,
                Radius = 1,
                Speed = float.MaxValue,
                From = pos,
                Range = float.MaxValue,
                Collision = spell.Collision,
                Type = spell.Type,
                RangeCheckFrom = pos,
                Aoe = aoe,
            });
        }

        protected Object[] VectorPointProjectionOnLineSegment(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float cx = v3.X;
            float cy = v3.Y;
            float ax = v1.X;
            float ay = v1.Y;
            float bx = v2.X;
            float by = v2.Y;
            float rL = ((cx - ax) * (bx - ax) + (cy - ay) * (by - ay)) /
                       ((float)Math.Pow(bx - ax, 2) + (float)Math.Pow(by - ay, 2));
            var pointLine = new Vector2(ax + rL * (bx - ax), ay + rL * (by - ay));
            float rS;
            if (rL < 0)
            {
                rS = 0;
            }
            else if (rL > 1)
            {
                rS = 1;
            }
            else
            {
                rS = rL;
            }
            bool isOnSegment = rS.CompareTo(rL) == 0;
            Vector2 pointSegment = isOnSegment ? pointLine : new Vector2(ax + rS * (bx - ax), ay + rS * (@by - ay));
            return new object[] { pointSegment, pointLine, isOnSegment };
        }

        protected void CastBasicSkillShot(Spell spell, float range, TargetSelector.DamageType type, HitChance hitChance, bool towerCheck = false)
        {
            var target = TargetSelector.GetTarget(range, type);

            if (target == null || !spell.IsReady())
                return;

            if (towerCheck && target.UnderTurret(true))
                return;

            spell.UpdateSourcePosition();

            if (spell.GetPrediction(target).Hitchance >= hitChance)
                spell.Cast(target, packets());
        }

        protected void CastBasicFarm(Spell spell)
        {
            if(!spell.IsReady())
				return;
            var minion = MinionManager.GetMinions(Player.ServerPosition, spell.Range, MinionTypes.All, MinionTeam.NotAlly);

            if (minion.Count == 0)
                return;

            if (spell.Type == SkillshotType.SkillshotCircle)
            {
                spell.UpdateSourcePosition();

                var predPosition = spell.GetCircularFarmLocation(minion);

                if (predPosition.MinionsHit >= 2)
                {
                    spell.Cast(predPosition.Position, Player.ChampionName == "Kartus" || packets());
                }
            }
            else if (spell.Type == SkillshotType.SkillshotLine)
            {
                spell.UpdateSourcePosition();

                var predPosition = spell.GetLineFarmLocation(minion);

                if(predPosition.MinionsHit >= 2)
                    spell.Cast(predPosition.Position, packets());
            }
        }

        protected Obj_AI_Hero GetTargetFocus(float range)
        {
            var focusSelected = menu.Item("selected", true).GetValue<bool>();

            if (TargetSelector.GetSelectedTarget() != null)
                if (focusSelected && TargetSelector.GetSelectedTarget().Distance(Player.ServerPosition) < range + 100 && TargetSelector.GetSelectedTarget().Type == GameObjectType.obj_AI_Hero)
                {
                    //Game.PrintChat("Focusing: " + TargetSelector.GetSelectedTarget().Name);
                    return TargetSelector.GetSelectedTarget();
                }
            return null;
        }

        protected HitChance GetHitchance(string source)
        {
            var hitC = HitChance.High;
            int qHit = menu.Item("qHit", true).GetValue<Slider>().Value;
            int harassQHit = menu.Item("qHit2", true).GetValue<Slider>().Value;

            // HitChance.Low = 3, Medium , High .... etc..
            if (source == "Combo")
            {
                switch (qHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }
            else if (source == "Harass")
            {
                switch (harassQHit)
                {
                    case 1:
                        hitC = HitChance.Low;
                        break;
                    case 2:
                        hitC = HitChance.Medium;
                        break;
                    case 3:
                        hitC = HitChance.High;
                        break;
                    case 4:
                        hitC = HitChance.VeryHigh;
                        break;
                }
            }

            return hitC;
        }
        protected void AddManaManagertoMenu(Menu myMenu, String source, int standard)
        {
            myMenu.AddItem(new MenuItem(source + "_Manamanager", "蓝量 管理", true).SetValue(new Slider(standard)));
        }

        protected bool FullManaCast()
        {
            if (Player.Mana >= QSpell.ManaCost + WSpell.ManaCost + ESpell.ManaCost + RSpell.ManaCost)
                return true;
            return false;
        }

        protected bool HasMana(string source)
        {
            if (Player.ManaPercentage() > menu.Item(source + "_Manamanager", true).GetValue<Slider>().Value)
                return true;
            return false;
        }

        //to create by champ
        protected virtual void Drawing_OnDraw(EventArgs args)
        {
            //for champs to use
        }

        protected virtual void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            //for champs to use
        }

        protected virtual void Interrupter_OnPosibleToInterrupt(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs spell)
        {
            //for champs to use
        }

        protected virtual void Game_OnGameUpdate(EventArgs args)
        {
            //for champs to use
        }

        protected virtual void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //for champs to use
        }

        protected virtual void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            //for champs to use
        }

        protected virtual void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            //for champ use
        }

        protected virtual void Game_OnSendPacket(GamePacketEventArgs args)
        {
            //for champ use
        }

        protected virtual void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            //for champ use
        }

        protected virtual void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            //for champ use
        }

        protected virtual void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            //for champ use
        }

        protected virtual void BeforeAttack(xSLxOrbwalker.BeforeAttackEventArgs args)
        {
            //for champ use
        }

        protected virtual void ObjAiHeroOnOnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            //for champ use
        }

        protected virtual void Spellbook_OnUpdateChargedSpell(Spellbook sender, SpellbookUpdateChargedSpellEventArgs args)
        {
            //for champ use
        }
    }
}
