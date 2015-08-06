﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using GamePath = System.Collections.Generic.List<SharpDX.Vector2>;

namespace PRADA_Vayne.Utils
{
    internal static class Extensions
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;

        public static bool IsCondemnable(this Obj_AI_Hero hero)
        {
            if (!hero.IsValidTarget(550f) || hero.HasBuffOfType(BuffType.SpellShield) ||
                hero.HasBuffOfType(BuffType.SpellImmunity) || hero.IsDashing()) return false;

            //values for pred calc pP = player position; p = enemy position; pD = push distance
            var pP = Heroes.Player.ServerPosition;
            var p = hero.ServerPosition;
            var pD = Program.ComboMenu.Item("EPushDist").GetValue<Slider>().Value;
            var mode = Program.ComboMenu.Item("EMode").GetValue<StringList>().SelectedValue;

            if (mode == "PRADA" &&
                (p.Extend(pP, -pD).IsCollisionable() || p.Extend(pP, -pD/2f).IsCollisionable() ||
                 p.Extend(pP, -pD/3f).IsCollisionable()))
            {
                if (!hero.CanMove ||
                    (hero.IsWindingUp && Program.ComboMenu.Item("EHitchance").GetValue<Slider>().Value < 100))
                    return true;

                if (Program.ComboMenu.Item("EHitchance").GetValue<Slider>().Value <= 85)
                {
                    var prediction = Program.E.GetPrediction(hero);
                    for (var i = 15; i < pD; i += 75)
                    {
                        var posCF = NavMesh.GetCollisionFlags(
                            prediction.UnitPosition.To2D()
                                .Extend(
                                    pP.To2D(),
                                    -i)
                                .To3D());
                        if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                var eT = 0.063 + Game.Ping/2000f + 0.06;
                eT += (double) Program.ComboMenu.Item("EHitchance").GetValue<Slider>().Value*4/1000;
                var d = hero.MoveSpeed*eT;

                var pList = new List<Vector3>();
                pList.Add(hero.ServerPosition);


                for (var i = 0; i <= 360; i += 60)
                {
                    var v3 = new Vector2((int) (p.X + d*Math.Cos(i)), (int) (p.Y - d*Math.Sin(i))).To3D();
                    pList.Add(v3.Extend(pP, -pD));
                }

                return pList.All(el => el.IsCollisionable());
            }

            if (mode == "MARKSMAN")
            {
                var prediction = Program.E.GetPrediction(hero);
                return NavMesh.GetCollisionFlags(
                    prediction.UnitPosition.To2D()
                        .Extend(
                            pP.To2D(),
                            -pD)
                        .To3D()).HasFlag(CollisionFlags.Wall) ||
                       NavMesh.GetCollisionFlags(
                           prediction.UnitPosition.To2D()
                               .Extend(
                                   pP.To2D(),
                                   -pD/2f)
                               .To3D()).HasFlag(CollisionFlags.Wall);
            }

            if (mode == "GOSU")
            {
                var prediction = Program.E.GetPrediction(hero);
                for (var i = 15; i < pD; i += 100)
                {
                    var posCF = NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(
                                pP.To2D(),
                                -i)
                            .To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == "SHARPSHOOTER")
            {
                var prediction = Program.E.GetPrediction(hero);
                for (var i = 15; i < pD; i += 75)
                {
                    var posCF = NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(
                                pP.To2D(),
                                -i)
                            .To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (mode == "VHREWORK")
            {
                var prediction = Program.E.GetPrediction(hero);
                for (var i = 15; i < pD; i += (int) hero.BoundingRadius) //:frosty:
                {
                    var posCF = NavMesh.GetCollisionFlags(
                        prediction.UnitPosition.To2D()
                            .Extend(
                                pP.To2D(),
                                -i)
                            .To3D());
                    if (posCF.HasFlag(CollisionFlags.Wall) || posCF.HasFlag(CollisionFlags.Building))
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        public static Vector3 GetTumblePos(this Obj_AI_Hero target)
        {
            //if the target is not a melee and he's alone he's not really a danger to us, proceed to 1v1 him :^ )
            if (!target.IsMelee && Heroes.Player.CountEnemiesInRange(800) == 1) return Game.CursorPos;

            var flash = Program.Flash;
            var flashedAtTick = Program.FlashTime;
            if (!flash.IsReady())
            {
                if (Environment.TickCount - flashedAtTick < 500) return Vector3.Zero;
            }

            var aRC = new Geometry.Circle(Heroes.Player.ServerPosition.To2D(), 300).ToPolygon().ToClipperPath();
            var cP = Game.CursorPos;
            var tP = target.ServerPosition;
            var pList = new List<Vector3>();
            var additionalDistance = (0.106 + Game.Ping/2000f) * target.MoveSpeed;

            if ((!cP.IsWall() && !cP.UnderTurret(true) && cP.Distance(tP) > 325 && cP.Distance(tP) < 550 &&
                 (cP.CountEnemiesInRange(425) <= cP.CountAlliesInRange(325)))) return cP;

            foreach (var p in aRC)
            {
                var v3 = new Vector2(p.X, p.Y).To3D();

                if (target.IsFacing(Heroes.Player))
                {
                    if (!v3.IsWall() && !v3.UnderTurret(true) && v3.Distance(tP) > 325 && v3.Distance(tP) < 550 &&
                        (v3.CountEnemiesInRange(425) <= v3.CountAlliesInRange(325))) pList.Add(v3);
                }
                else
                {
                    if (!v3.IsWall() && !v3.UnderTurret(true) && v3.Distance(tP) > 325 &&
                        v3.Distance(tP) < (550 - additionalDistance) &&
                        (v3.CountEnemiesInRange(425) <= v3.CountAlliesInRange(325))) pList.Add(v3);
                }
            }
            if (Heroes.Player.UnderTurret() || Heroes.Player.CountEnemiesInRange(800) == 1)
            {
                return pList.Count > 1 ? pList.OrderBy(el => el.Distance(cP)).FirstOrDefault() : Vector3.Zero;
            }
            return pList.Count > 1 ? pList.OrderByDescending(el => el.Distance(tP)).FirstOrDefault() : Vector3.Zero;
        }

        public static Vector3 GetTumblePos(this Vector3 tP)
        {
            var aRC = new Geometry.Circle(Heroes.Player.ServerPosition.To2D(), 300).ToPolygon().ToClipperPath();
            var cP = Game.CursorPos;
            var pList = new List<Vector3>();
            var minDist = Program.ComboMenu.Item("QMinDist").GetValue<Slider>().Value;

            if ((!cP.IsWall() && !cP.UnderTurret(true) && cP.Distance(tP) > minDist && cP.Distance(tP) < 550 &&
                 (cP.CountEnemiesInRange(425) <= cP.CountAlliesInRange(325)))) return cP;

            foreach (var p in aRC)
            {
                var v3 = new Vector2(p.X, p.Y).To3D();

                if (!v3.IsWall() && !v3.UnderTurret(true) && v3.Distance(tP) > minDist && v3.Distance(tP) < 550 &&
                    (v3.CountEnemiesInRange(425) <= v3.CountAlliesInRange(325))) pList.Add(v3);
            }
            if (Heroes.Player.UnderTurret() || Heroes.Player.CountEnemiesInRange(800) == 1)
            {
                return pList.Count > 1 ? pList.OrderBy(el => el.Distance(cP)).FirstOrDefault() : Vector3.Zero;
            }
            return pList.Count > 1 ? pList.OrderByDescending(el => el.Distance(tP)).FirstOrDefault() : Vector3.Zero;
        }

        public static Vector3 GetCondemnPosition(this Vector3 position)
        {
            var pointList = new List<Vector3>();

            pointList.Add(Vector3.Zero);

            for (var j = 485; j >= 50; j -= 100)
            {
                var offset = (int)(2 * Math.PI * j / 100);

                for (var i = 0; i <= offset; i++)
                {
                    var angle = i * Math.PI * 2 / offset;
                    var point =
                        new Vector2(
                            (float)(position.X + j * Math.Cos(angle)),
                            (float)(position.Y - j * Math.Sin(angle))).To3D();

                    var cP = point.Extend(position, point.Distance(position) + 50);
                    if (point.IsWall() && cP.Distance(point) < 425 && !cP.UnderTurret(true) && cP.Distance(position) > 325 && cP.Distance(position) < 545 &&
                 (cP.CountEnemiesInRange(425) <= cP.CountAlliesInRange(325)))
                    {
                        pointList.Add(cP);
                    }
                }
            }

            return pointList.OrderByDescending(p=>p.Distance(position)).FirstOrDefault();
        }

        public static int VayneWStacks(this Obj_AI_Base o)
        {
            if (o == null) return 0;
            if (o.Buffs.FirstOrDefault(b => b.Name.Contains("vaynesilver")) == null || !o.Buffs.Any(b => b.Name.Contains("vaynesilver"))) return 0;
            return o.Buffs.FirstOrDefault(b => b.Name.Contains("vaynesilver")).Count;
        }

        public static Vector3 Randomize(this Vector3 pos)
        {
            var r = new Random(Environment.TickCount);
            return new Vector2(pos.X + r.Next(-150, 150), pos.Y + r.Next(-150, 150)).To3D();
        }

        public static bool IsShroom(this Vector3 pos)
        {
            return pos == Vector3.Zero ||
                   HeroManager.Enemies.Any(e => !e.IsDead && e.IsVisible && e.Distance(pos) < Program.ComboMenu.Item("QMinDist").GetValue<Slider>().Value) ||
                   Traps.EnemyTraps.Any(t => pos.Distance(t.Position) < 125);
        }

        public static bool IsKillable(this Obj_AI_Hero hero)
        {
            return Player.GetAutoAttackDamage(hero) * 2 < hero.Health;
        }

        public static bool IsCollisionable(this Vector3 pos)
        {
            return NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall) ||
                (Program.Orbwalker.ActiveMode == MyOrbwalker.OrbwalkingMode.Combo && NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Building));
        }
        public static bool IsValidState(this Obj_AI_Hero target)
        {
            return !target.HasBuffOfType(BuffType.SpellShield) && !target.HasBuffOfType(BuffType.SpellImmunity) &&
                   !target.HasBuffOfType(BuffType.Invulnerability);
        }

        public static int CountHerosInRange(this Obj_AI_Hero target, bool checkteam, float range = 1200f)
        {
            var objListTeam =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        x => x.IsValidTarget(range, false));

            return objListTeam.Count(hero => checkteam ? hero.Team != target.Team : hero.Team == target.Team);
        }
    }
}
