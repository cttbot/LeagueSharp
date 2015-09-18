﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using DRAVEN_Draven.MyUtils;

namespace DRAVEN_Draven.MyLogic.E
{
    public static partial class Events
    {
        public static void OnUpdate(EventArgs args)
        {
            if (Program.E.IsReady())
            {
                var chasee = Heroes.EnemyHeroes.FirstOrDefault(e => e.HealthPercent < 50 && !e.IsFacing(Heroes.Player));
                if (ObjectManager.Player.CountEnemiesInRange(1200) <= 2 && chasee != null)
                {
                    Program.E.Cast(chasee);
                }
            }
        }
    }
}