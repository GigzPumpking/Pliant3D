using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ObjectiveLookup
{
    /// <summary>
    /// Checks whether the passed GameObject is part of any currently tracked objective.
    /// Returns (true, objective) if found, otherwise (false, null).
    /// </summary>
    public static (bool found, Objective objective) TryGetObjectiveForGameObject(GameObject target)
    {
        if (!target)
        {
            return (false, null);
        }

        foreach (Objective objective in GetCurrentlyTrackedObjectives())
        {
            if (!objective) continue;

            if (ObjectiveUsesGameObject(objective, target))
            {
                return (true, objective);
            }
        }

        return (false, null);
    }
    
    public static bool TryGetObjectiveForGameObject(GameObject target, out Objective objective)
    {
        var result = TryGetObjectiveForGameObject(target);

        objective = result.objective;
        return result.found;
    }

    private static IEnumerable<Objective> GetCurrentlyTrackedObjectives()
    {
        HashSet<Objective> objectives = new HashSet<Objective>();

        // Primary source: objectives that currently have UI.
        foreach (Objective objective in ObjectiveListing.ObjectiveToUI.Keys.ToList())
        {
            if (objective)
            {
                objectives.Add(objective);
            }
        }

        // Fallback source: active ObjectiveListings in the scene.
        foreach (ObjectiveListing listing in GameObject.FindObjectsOfType<ObjectiveListing>())
        {
            if (!listing || listing.objectives == null) continue;

            foreach (Objective objective in listing.objectives)
            {
                if (objective)
                {
                    objectives.Add(objective);
                }
            }
        }

        return objectives;
    }

    private static bool ObjectiveUsesGameObject(Objective objective, GameObject target)
    {
        if (!objective || !target)
        {
            return false;
        }

        // If the objective component itself is on the target or related object.
        if (SameObjectOrHierarchy(objective.gameObject, target))
        {
            return true;
        }

        Type objectiveType = objective.GetType();

        FieldInfo[] fields = objectiveType.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(objective);

            if (ValueMatchesGameObject(value, target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ValueMatchesGameObject(object value, GameObject target)
    {
        if (value == null || !target)
        {
            return false;
        }

        // GameObject field.
        if (value is GameObject gameObject)
        {
            return SameObjectOrHierarchy(gameObject, target);
        }

        // Component field, like Transform, DialogueTrigger, Interactable, Collider, etc.
        if (value is Component component)
        {
            return component && SameObjectOrHierarchy(component.gameObject, target);
        }

        // Objective field.
        if (value is Objective objective)
        {
            return objective && SameObjectOrHierarchy(objective.gameObject, target);
        }

        // Lists/arrays of GameObjects, Components, Objectives, etc.
        if (value is IEnumerable enumerable && value is not string)
        {
            foreach (object item in enumerable)
            {
                if (ValueMatchesGameObject(item, target))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool SameObjectOrHierarchy(GameObject a, GameObject b)
    {
        if (!a || !b)
        {
            return false;
        }

        if (a == b)
        {
            return true;
        }

        Transform aTransform = a.transform;
        Transform bTransform = b.transform;

        // Handles child colliders / child visuals.
        if (aTransform.IsChildOf(bTransform))
        {
            return true;
        }

        if (bTransform.IsChildOf(aTransform))
        {
            return true;
        }

        return false;
    }
}