using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace TaxiSystem
{
    public class Main : BaseScript
    {

        int blip = 0;

        Vehicle vehicle;

        bool needTaxi = false;

        public Main()
        {
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
            Tick += OnTick;
        }

        public Task OnTick()
        {
            if(needTaxi && Vector3.Distance(Game.PlayerPed.Position, vehicle.Position) < 7 && !Game.PlayerPed.IsInVehicle() )
            {
                needTaxi = false;
                Game.PlayerPed.Task.EnterVehicle(vehicle, VehicleSeat.Passenger);
            }
            if(DoesBlipExist(blip))
            {
                if (Game.PlayerPed.IsInVehicle())
                {
                    RemoveBlip(ref blip);
                }
            }

            return null;
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName)
            {
                return;
            }

            RegisterCommand("calltaxi", new Action<int, List<object>, string> (async (i, args, s) => {

                ExecuteCommand($"postal {args[0].ToString()}");

                Debug.WriteLine($"{Game.Player.Name} called a taxi");

                var spawn = new Vector3();

                float head = 0;
                int unused = 0;

                var player = Game.PlayerPed;

                GetNthClosestVehicleNodeWithHeading(player.Position.X, player.Position.Y, player.Position.Z, 50, ref spawn, ref head, ref unused, 9, 3.0f, 2.5f);

                await LoadModel((uint)VehicleHash.Taxi);
                vehicle = await World.CreateVehicle(VehicleHash.Taxi, spawn, head);

                blip = AddBlipForEntity(vehicle.Handle);
                SetBlipColour(blip, (int)BlipColor.Yellow);
                BeginTextCommandSetBlipName("STRING");
                AddTextComponentString("Taxi");
                EndTextCommandSetBlipName(blip);


                await LoadModel((uint)PedHash.LamarDavis);
                var taxiPed = await World.CreatePed(PedHash.LamarDavis, spawn);
                taxiPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);
                taxiPed.CanBeTargetted = false;
                taxiPed.CanBeDraggedOutOfVehicle = false;
                SetPedCombatAbility(taxiPed.Handle, 40);
                var targetLoc = new Vector3();
                float targetHeading = 0;

                GetClosestVehicleNodeWithHeading(player.Position.X, player.Position.Y, player.Position.Z, ref targetLoc, ref targetHeading, 1, 3.0f, 0);
                taxiPed.Task.DriveTo(vehicle, targetLoc, 5, 20, 786603);
                
                while (Vector3.Distance(targetLoc, vehicle.Position) > 5)
                {
                    await Delay(2000);
                    Screen.ShowNotification("Taxi is driving to your location");
                }

                Screen.ShowNotification("Walk towards the taxi to get in", true);


                //var postalsJson = File.ReadAllText("postals.txt");

                //var postals = JsonConvert.DeserializeObject<Postal[]>(postalsJson);

                //var yes = postals.Where(e => e.postal == args[0].ToString()).First();

                //Screen.ShowNotification($"x = {yes.x} || y = {yes.y} || postal = {yes.postal}");

                await Delay(4000);

                if (vehicle.PassengerCount > 1)
                {
                    taxiPed.Task.DriveTo(vehicle, new Vector3(104.07f, -1011.76f, 28.72f), 5, 40, 786603);
                }
                else
                {
                    taxiPed.Task.DriveTo(vehicle, new Vector3(104.07f, -1011.76f, 28.72f), 5, 40, 786603);
                }

                needTaxi = true;

            }), false);

            RegisterCommand("car", new Action<int, List<object>, string>(async (i, args, s) =>
            {
                if (!Game.PlayerPed.IsInVehicle())
                {
                    if (args.Count == 1)
                    {
                        var hash = (uint)GetHashKey(args[0].ToString());
                        if (!IsModelInCdimage(hash) || !IsModelAVehicle(hash))
                        {
                            Screen.ShowNotification("[CarSpawner] That vehicle does not exist!");
                        }
                        else
                        {
                            var vehicle = await World.CreateVehicle(args[0].ToString(), Game.PlayerPed.Position, Game.PlayerPed.Heading);

                            Game.PlayerPed.SetIntoVehicle(vehicle, VehicleSeat.Driver);

                            Screen.ShowNotification($"[CarSpawner] Spawned {args[0]}");
                        }
                    }
                    else
                    {
                        Screen.ShowNotification("[CarSpawner] Please provide 1 argument!");
                    }
                }
                else
                {
                    Screen.ShowNotification("[CarSpawner] You are already in a vehicle!");
                }




            }), false);

            //RegisterCommand("fire", new Action<int, List<object>, string>((i, args, s) =>
            //{
            //    var player = Game.PlayerPed;
            //    //StartEntityFire(GetClosestVehicle(player.Position.X, player.Position.Y, player.Position.Z, 5, 0, 0));

            //    World.AddExplosion(player.Position, ExplosionType.Train, 1, 2f, null, true);

            //}), false);

        }

        private async Task<bool> LoadModel(uint model)
        {
            if (!IsModelInCdimage(model))
            {
                Debug.WriteLine($"Model ({model}) does not exist");
                return false;
            }
            API.RequestModel(model);
            while (!HasModelLoaded(model))
            {
                Debug.WriteLine($"Model ({model}) is loading");
                await Delay(1000);
            }
            return true;
        }
    }
}
