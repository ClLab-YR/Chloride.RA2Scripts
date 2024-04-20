﻿using Chloride.RA2Scripts.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chloride.RA2Scripts;
internal class OwnerMapScript
{
    /// <summary>
    /// Key for ID, Value for house param index.
    /// </summary>
    internal Dictionary<string, int> ActionsWithOwner = new();
    internal Dictionary<string, int> EventsWithOwner = new();
    internal List<string> ScriptsWithOwner = new();

    internal OwnerMapScript(IniDoc config)
    {
        _ = config.Default.Contains("ExtOwnerScripts", out IniValue extScripts);
        ScriptsWithOwner = ScriptsWithOwner.Union(extScripts.Split()).ToList();

        _ = config.Contains("ExtOwnerEvents", out IniSection? extEvents);
        foreach (var i in extEvents ?? new(string.Empty))
        {
            EventsWithOwner.Add(i.Key, i.Value.Convert<int>());
        }

        _ = config.Contains("ExtOwnerActions", out IniSection? extActions);
        foreach (var i in extActions ?? new(string.Empty))
        {
            ActionsWithOwner.Add(i.Key, i.Value.Convert<int>());
        }
    }

    /* step 1: check out alliance （TODO
     * step 2: check out easy refs （Done
     *  - 2.1 teams (House=
     *  - 2.2 triggers (string[0])
     *  - 2.3 technos (string[0])
     *  
     * step 3: check out int args
     *  3.1 events, actions
     *  3.2 scripts (20
     */

    /// <summary>
    /// No need to input "XX House", just "XX".
    /// </summary>
    internal static void TransferOwnerAlliance(IniDoc doc, string old, string _new)
    {
        old = $"{old} House";
        _new = $"{_new} House";
        if (!doc.Contains(old, out IniSection? oldsect) || !doc.Contains(_new, out IniSection? newsect))
            return;

        // transfer old alliance
        List<string> newRelationships = newsect!["Allies"].Split().ToList();
        foreach (var i in oldsect!["Allies"].Split())
        {
            if (i == old)
                continue;
            if (newRelationships.Contains(i))
                continue;
            newRelationships.Add(i);
        }
        newsect["Allies"] = IniValue.Join<string>(newRelationships);

        // change all other houses alliance.
        foreach (var i in doc.GetTypeList("Houses"))
        {
            if (i == old)
                continue;
            if (!doc.Contains(i, out IniSection? iHouse))
                continue;
            var iRelationships = iHouse!["Allies"].Split();
            for (int j = 0; j < iRelationships.Length; j++)
            {
                if (iRelationships[j] != old)
                    continue;
                iRelationships[j] = _new;
                break;
            }
        }
    }

    /// <summary>
    /// No need to input "XX House", just "XX".
    /// </summary>
    internal void TransferOwnerReference(IniDoc doc, string old, string _new)
    {
        var h_old = $"{old} House";
        var h_new = $"{_new} House";

        // by name
        foreach (var i in doc.GetTypeList("TeamTypes"))
        {
            if (!doc.Contains(i, out IniSection? team))
                continue;
            if (team!.Contains("House", out IniValue teamHouse) && teamHouse.ToString() != old)
                continue;
            team["House"] = _new;
        }

        TechnosMapScript.Replacement(doc, "Triggers", triggerinfo =>
        {
            if (triggerinfo[0] == old)
                triggerinfo[0] = _new;
        });

        TechnosMapScript.Replacement(doc, "Infantry", techno =>
        {
            if (techno[0] == h_old)
                techno[0] = h_new;
        });
        TechnosMapScript.Replacement(doc, "Units", techno =>
        {
            if (techno[0] == h_old)
                techno[0] = h_new;
        });
        TechnosMapScript.Replacement(doc, "Aircraft", techno =>
        {
            if (techno[0] == h_old)
                techno[0] = h_new;
        });
        TechnosMapScript.Replacement(doc, "Structures", techno =>
        {
            if (techno[0] == h_old)
                techno[0] = h_new;
        });

        // by int args
        var houses = doc.GetTypeList("Houses");
        var idxOld = Array.IndexOf(houses, h_old);
        var idxNew = Array.IndexOf(houses, h_new);

        TechnosMapScript.Replacement(doc, "Actions", tActions =>
        {
            //# triggerID = actionsCount, (actionID,p1,p2,p3,p4,p5,p6,p7), ...
            var cnt = int.Parse(tActions[0]);
            for (int i = 0, k = 1; i < cnt; i++, k += 8)
            {
                var curAID = tActions[k];
                if (!ActionsWithOwner.TryGetValue(curAID, out int idx))
                    continue;
                if (tActions[k + idx] != idxOld.ToString())
                    continue;
                tActions[k + idx] = idxNew.ToString();
            }
        });

        TechnosMapScript.Replacement(doc, "Events", tEvents =>
        {
            //triggerID = eventsCount, (eventID,tag,p1), (eventID,tag,p1,[optional p2]), ...
            var cnt = int.Parse(tEvents[0]);  // ec
            for (int i = 0, k = 1; i < cnt; i++)
            {
                var curEID = tEvents[k]; // eid
                var specialTag = int.Parse(tEvents[++k]); // tag
                if (EventsWithOwner.TryGetValue(curEID, out int idx) && tEvents[k + idx] == idxOld.ToString())
                {
                    tEvents[k + idx] = idxNew.ToString();
                }
                k += specialTag switch
                {
                    2 => 3,
                    _ => 2,
                };
            }
        });

        foreach (var i in doc.GetTypeList("ScriptTypes"))
        {
            TechnosMapScript.Replacement(doc, i, cur =>
            {
                if (cur.Length < 2)
                    return;
                if (ScriptsWithOwner.Contains(cur[0]) && cur[1] == idxOld.ToString())
                    cur[1] = idxNew.ToString();
            });
        }
    }
}