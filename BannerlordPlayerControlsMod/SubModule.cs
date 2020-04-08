using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;
using Module = TaleWorlds.MountAndBlade.Module;

namespace BannerlordPlayerControlsMod
{

    internal class DirectionalAttackKeys
    {
        internal enum Key: int
        {
            AttackUp = 72,
            AttackLeft,
            AttackRight,
            AttackDown,
        }

        internal static IEnumerable<Key> AllKeys = new[] {Key.AttackUp, Key.AttackLeft, Key.AttackRight, Key.AttackDown};

        internal static Agent.UsageDirection UsageDirectionFrom(Key key)
        {
            switch (key)
            {
                case Key.AttackUp:
                    return Agent.UsageDirection.AttackUp;
                case Key.AttackLeft:
                    return Agent.UsageDirection.AttackLeft;
                case Key.AttackRight:
                    return Agent.UsageDirection.AttackRight;
                case Key.AttackDown:
                    return Agent.UsageDirection.AttackDown;
                default:
                    return default(Agent.UsageDirection);
            }
        }

        internal static string NameFor(Key key)
        {
            switch (key)
            {
                case Key.AttackUp:
                    return "AttackDirectionUp";
                case Key.AttackLeft:
                    return "AttackDirectionLeft";
                case Key.AttackRight:
                    return "AttackDirectionRight";
                case Key.AttackDown:
                    return "AttackDirectionDown";
                default:
                    return null;
            }
        }
    }
    
    [HarmonyPatch(typeof(MissionMainAgentController), "ControlTick")]
    public static class MissionMainAgentControllerPatch
    {
        public static void Postfix(MissionMainAgentController __instance)
        {
            var agent = __instance.Mission.MainAgent;

            foreach (var key in DirectionalAttackKeys.AllKeys)
            {
                if (__instance.Input.IsGameKeyDown((int) key))
                {
                    agent.MovementFlags |= agent.AttackDirectionToMovementFlag(DirectionalAttackKeys.UsageDirectionFrom(key));
                }
            }
        }
    }
    
    public class Submodule: MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            var category = HotKeyManager.GetCategory("CombatHotKeyCategory") as CombatHotKeyCategory;
            var method = category.GetType().BaseType.GetMethod("RegisterGameKey", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            var list = category.PrivateValue<List<GameKey>>("_registeredGameKeys", typeof(GameKeyContext));
            list.AddRange(Enumerable.Repeat<GameKey>(null, 64));

            var nameText = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name");
            var descriptionText = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description");

            foreach (var key in DirectionalAttackKeys.AllKeys)
            {
                nameText.AddVariationWithId(nameof(CombatHotKeyCategory) + "_" + (int)key, new TextObject(DirectionalAttackKeys.NameFor(key)), new List<GameTextManager.ChoiceTag>());
                descriptionText.AddVariationWithId(nameof(CombatHotKeyCategory) + "_" + (int)key, new TextObject(DirectionalAttackKeys.NameFor(key)), new List<GameTextManager.ChoiceTag>());
                
                method.Invoke(category, new object[] 
                {
                    new GameKey(
                        (int)key,
                        DirectionalAttackKeys.NameFor(key),
                        "CombatHotKeyCategory", 
                        InputKey.Numpad8 + ((int)key - (int)DirectionalAttackKeys.Key.AttackUp),
                        InputKey.Invalid, 
                        GameKeyMainCategories.ActionCategory
                    ),
                    true
                });
            }
            
            new Harmony("net.shdw.BannerlordPlayerControlsMod").PatchAll();
            HotKeyManager.Load();
        }
    }
    
    public static class ReflectionHelpers
    {
        public static T PrivateValue<T>(this object o, string fieldName, Type t = null)
        {
            if (t == null)
            {
                t = o.GetType();
            }
            
            var field = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (T) field.GetValue(o);
        }

        public static void PrivateValueSet<T>(this object o, string fieldName, T value)
        {
            var field = o.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            field.SetValue(o, value);
        }
    }
}
