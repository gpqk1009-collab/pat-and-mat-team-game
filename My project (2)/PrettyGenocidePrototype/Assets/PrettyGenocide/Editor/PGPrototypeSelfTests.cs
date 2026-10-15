#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PrettyGenocide.Editor
{
    public static class PGPrototypeSelfTests
    {
        [MenuItem("Tools/Pretty Genocide/Run Ammo Ledger Self Tests")]
        public static void Run()
        {
            PGBulletLedger ledger = new PGBulletLedger(6);
            Require(ledger.Loaded == 6 && ledger.Outstanding == 0, "Initial cylinder");
            List<int> ids = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                Require(ledger.TryIssue(out int id), "Issue bullet " + i);
                Require(!ids.Contains(id), "Unique bullet ID");
                ids.Add(id);
                Require(ledger.Loaded + ledger.Outstanding == 6, "Capacity invariant");
            }
            Require(!ledger.TryIssue(out _), "Seventh bullet rejected");
            Require(!ledger.TryRecover(-100), "Unknown ID rejected");
            Require(ledger.TryRecover(ids[2]), "Known ID recovers");
            Require(!ledger.TryRecover(ids[2]), "Duplicate callback rejected");
            Require(ledger.Loaded == 1, "No duplicate ammunition");
            foreach (int id in ids) ledger.TryRecover(id);
            Require(ledger.Loaded == 6 && ledger.Outstanding == 0, "Full recovery");

            System.Random random = new System.Random(82471);
            HashSet<int> outside = new HashSet<int>();
            for (int i = 0; i < 10000; i++)
            {
                if (random.Next(2) == 0)
                {
                    bool issued = ledger.TryIssue(out int id);
                    Require(issued == (outside.Count < 6), "Issue availability");
                    if (issued) Require(outside.Add(id), "No reused outstanding ID");
                }
                else if (outside.Count > 0)
                {
                    int chosen = 0;
                    foreach (int id in outside) { chosen = id; break; }
                    Require(ledger.TryRecover(chosen), "Random recovery");
                    Require(!ledger.TryRecover(chosen), "Repeated random recovery rejected");
                    outside.Remove(chosen);
                }
                Require(ledger.Loaded == 6 - outside.Count, "Random loaded count");
                Require(ledger.Outstanding == outside.Count, "Random outstanding count");
            }
            Debug.Log("Pretty Genocide ammo ledger tests passed: six-chamber limit, unique IDs, duplicate recovery, 10,000 randomized operations. This does NOT test physics, input, animation, or camera behavior.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Ammo ledger test failed: " + message);
        }
    }
}
#endif
