
using Helpers.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SneakyTP.Patchs
{
    internal class PortalPatcher
    {
        public static void Patch()
        {
            On.Portal.OnTriggerEnter += Portal_OnTriggerEnter;
        }

        private static void Portal_OnTriggerEnter(On.Portal.orig_OnTriggerEnter orig, Portal self, UnityEngine.Collider other)
        {
            if (!self.Active)
            {
                return;
            }
            PlayerController component = other.gameObject.GetComponent<PlayerController>();
            if (component != null && !component.IsDead && self.HasStateAuthority)
            {
                List<Portal> list = (from p in UnityEngine.Object.FindObjectsOfType<Portal>().ToList()
                                     where p != self
                                     select p).ToList();
                if (list.Any())
                {
                    self.StartActivationTimer();
                    Portal portal = list.Grab(1).First();
                    portal.StartActivationTimer();
                    Transform transform = portal.teleportPoint;
                    Vector3 position = portal.transform.position;
                    self.Rpc_DisplayLight();
                    portal.Rpc_DisplayLight();
                    GameManager.Rpc_BroadcastFollowSound(self.Runner, "TELEPORT_START", self.transform.position, 30f);
                    component.CharacterMovementHandler.TeleportData = new NetworkTeleportData(transform.position, transform.rotation, resetLook: true);
                }
            }
        }
    }
}
