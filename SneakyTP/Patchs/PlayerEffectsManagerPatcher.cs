using Fusion;
using Helpers.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SneakyTP.Patchs
{
    internal class PlayerEffectsManagerPatcher
    {
        public static void Patch()
        {
            On.PlayerEffectsManager.CheckTeleportation += PlayerEffectsManager_CheckTeleportation;
        }

        private static void PlayerEffectsManager_CheckTeleportation(On.PlayerEffectsManager.orig_CheckTeleportation orig, PlayerEffectsManager self)
        {
            if (!self.HasStateAuthority || !self.TeleportationTimer.Expired(self.Runner) || (bool)self._playerController.IsDead)
            {
                return;
            }
            float delayInSeconds;
            if (self.PlayerPositions.Any())
            {
                Dictionary<PlayerController, Vector3> dictionary = new Dictionary<PlayerController, Vector3>();
                foreach (KeyValuePair<PlayerRef, Vector3> playerPosition in self.PlayerPositions)
                {
                    PlayerController player = PlayerRegistry.GetPlayer(playerPosition.Key);
                    if (player != null)
                    {
                        Vector3 position = player.transform.position;
                        Vector3 oldPosition = playerPosition.Value;
                        if (!PlayerRegistry.Any((PlayerController p) => Vector3.Distance(p.transform.position, oldPosition) < 0.5f) && Vector3.Distance(position, oldPosition) >= 0.5f)
                        {
                            dictionary.Add(player, oldPosition);
                        }
                    }
                }
                if (dictionary.Any())
                {
                    PlayerController playerController = dictionary.Keys.ToList().Grab(1).First();
                    Vector3 vector = dictionary[playerController];
                    Quaternion rotation = self._playerController.transform.rotation;
                    if (playerController != null && playerController.transform != null)
                    {
                        Vector3 forward = playerController.transform.position - vector;
                        forward.y = 0f;
                        rotation = Quaternion.LookRotation(forward);
                    }
                    GameManager.Rpc_BroadcastFollowSound(self.Runner, "TELEPORT_START", self._playerController.transform.position, 20f);
                    self._playerController.CharacterMovementHandler.TeleportData = new NetworkTeleportData(vector, rotation, resetLook: true);
                    self._playerController.IsClimbing = false;
                    delayInSeconds = UnityEngine.Random.Range(5f, 20f);
                }
                else
                {
                    delayInSeconds = 0.5f;
                }
                self.PlayerPositions.Clear();
            }
            else
            {
                foreach (PlayerController item in PlayerRegistry.Where((PlayerController p) => p != self._playerController && !p.IsDead && !p.IsClimbing && Vector3.Distance(self._playerController.transform.position, p.transform.position) >= 35f))
                {
                    self.PlayerPositions.Add(item.Ref, item.transform.position);
                }
                delayInSeconds = UnityEngine.Random.Range(0.1f, 1f);
            }
            self.TeleportationTimer = TickTimer.CreateFromSeconds(self.Runner, delayInSeconds);
        }
    }
}
