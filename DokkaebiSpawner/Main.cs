﻿using System;
using System.Collections.Generic;
using SharpConfig;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace DokkaebiSpawner
{
    public class Main : BaseScript
    {
        public static Dictionary<string, Dictionary<string, string>> VehicleDatabase { get; set; }
        public static List<string> Categories { get; set; }
        public static string[] Disabled { get; set; }
        public Main()
        {
            VehicleDatabase = new Dictionary<string, Dictionary<string, string>> { };
            Categories = new List<string> { };
            Disabled = new string[] { };
            ConfigurationReader();
        }

        private void MenuCommand(string name)
        {
            TriggerEvent("chat:addSuggestion", "/" + name, "Open vehicle spawner menu");
            RegisterCommand(name, new Action<string, List<object>, string>((source, args, rawCommand) =>
            {
                Menu.Toggle();
            }), false);
        }

        private void ConfigurationReader()
        {
            var data = LoadResourceFile(GetCurrentResourceName(), "config.ini");
            if (Configuration.LoadFromString(data).Contains("VehicleSpawner") == true)
            {
                Configuration loaded = Configuration.LoadFromString(data);
                Disabled = loaded["VehicleSpawner"]["DisabledVehicles"].StringValueArray;
                MenuCommand(loaded["VehicleSpawner"]["CommandName"].StringValue);
                foreach (var setting in loaded)
                {
                    if (setting.Name != "VehicleSpawner")
                    {
                        var settingName = setting.Name;
                        foreach (var item in setting)
                        {
                            if(!item.IsEmpty)
                            {
                                if(!IsModelValid((uint)GetHashKey(item.Name)))
                                {
                                    ProcessError($"The spawn code { item.Name } is invalid");
                                }
                                else
                                {
                                    var dictionary = new Dictionary<string, string> { };
                                    if (!Categories.Contains(setting.Name))
                                    {
                                        Categories.Add(setting.Name);
                                    }
                                    dictionary.Add(item.Name, setting.Name);
                                    if (!VehicleDatabase.ContainsKey(item.StringValue))
                                    {
                                        VehicleDatabase.Add(setting.Name + item.StringValue, dictionary);
                                    }
                                }
                            }
                            else
                            {
                                ProcessError();
                            }
                        }
                    }
                }
                Debug.WriteLine("Vehicle configuration loaded.");
            }
            else
            {
                ProcessError();
            }
        }

        private void ProcessError(string message = "The configuration file has not been configured correctly.")
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 153, 153 },
                multiline = true,
                args = new[] { "[DokkaebiSpawner]", message}
            });
        }

        public static async void SpawnVehicle(string vehicle)
        {
            var hash = (uint)GetHashKey(vehicle);
            var current = GetVehiclePedIsIn(PlayerPedId(), true);
            if (IsPedInAnyVehicle(PlayerPedId(), true))
            {
                SetEntityAsMissionEntity(current, false, false);
                DeleteEntity(ref current);
                DeleteVehicle(ref current);
            }
            RequestModel(hash);
            while (!HasModelLoaded(hash))
            {
                RequestModel(hash);
                await Delay(0);
            }
            var coords = GetOffsetFromEntityInWorldCoords(PlayerPedId(), 0, 5f, 0.1f);
            Vehicle spawn = new Vehicle(CreateVehicle(hash, coords.X, coords.Y, coords.Z + 1f, GetEntityHeading(PlayerPedId()), true, false))
            {
                NeedsToBeHotwired = false,
                PreviouslyOwnedByPlayer = true,
                IsPersistent = true,
                IsStolen = false,
                IsWanted = false
            };
            SetPedIntoVehicle(PlayerPedId(), spawn.Handle, -1);
            spawn.IsEngineRunning = true;
            spawn.PlaceOnGround();
        }
    }
}
