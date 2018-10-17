﻿#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteInEditMode]
public class Folder : MonoBehaviour {
#if UNITY_EDITOR
    private static bool addedSelectionResetCallback;
    Folder() {
        // add reset callback first in queue
        if (!addedSelectionResetCallback) {
            Selection.selectionChanged += () => Tools.hidden = false;
            addedSelectionResetCallback = true;
        }

        Selection.selectionChanged += HandleSelection;
    }

    /// <summary>
    /// <para>Hides the transform gizmo if necessary to avoid accidental editing of the
    /// position.</para>
    /// 
    /// <para>(If multiple objects are selected along with a folder, this turns off all of their
    /// gizmos.)</para>
    /// </summary>
    private void HandleSelection() {
        if (this != null) {
            Tools.hidden |= Selection.activeGameObject == gameObject;
        }
    }

    private bool AskDelete() {
        return EditorUtility.DisplayDialog(
            title: "Can't add script",
            message: "Folders shouldn't be used with other components. Which component should " +
            "be kept?",
            ok: "Folder",
            cancel: "Component"
        );
    }

    /// <summary>
    /// Delete all components regardless of dependency hierarchy.
    /// </summary>
    /// <param name="comps">Which components to delete.</param>
    private void DeleteComponents(IEnumerable<Component> comps) {
        var destroyable = comps.Where(c => c != null && c.gameObject.CanDestroy(c));

        // keep cycling through the list of components until all components are gone.
        while (destroyable.Any()) {
            foreach (var c in destroyable) {
                if (c.gameObject.CanDestroy(c)) {
                    DestroyImmediate(c);
                }
            }
        }
    }

    private void OnGUI() {
        // we are running, don't bother the player.
        // also, sometimes `this` might be null for whatever reason.
        if (Application.isPlaying || this == null) {
            return;
        }

        var existingComponents = GetComponents<Component>()
            .Where(c => c != this && !typeof(Transform).IsAssignableFrom(c.GetType()))
            .ToList();

        // no items means no actions anyways
        if (!existingComponents.Any()) return;

        if (AskDelete()) {
            DeleteComponents(existingComponents);
        } else {
            DestroyImmediate(this);
        }
    }

    private void OnEnable() {
        tag = "EditorOnly";

        // Hide inspector to prevent accidental editing of transform.
        this.transform.hideFlags = HideFlags.HideInInspector;
    }

    private void OnDestroy() {
        tag = "Untagged";
    }
#endif

    private void Update() {
        ResetTransform();

        if (Application.isPlaying) {
            FinishFlattening();
        }
    }

    /// <summary>
    /// Resets the transform properties to their identities, i.e. (0, 0, 0), (0˚, 0˚, 0˚), and
    /// (100%, 100%, 100%).
    /// </summary>
    private void ResetTransform() {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(1, 1, 1);
    }

    /// <summary>
    /// Destroy object as it should have been when building.
    /// </summary>
    private void FinishFlattening() {
        Debug.LogError("Folder \"" + name + "\" was supposed to be destroyed on build.");
        Flatten();
        Destroy(gameObject);
    }

    /// <summary>
    /// Takes direct children and links them to the parent transform or global.
    /// </summary>
    public void Flatten() {
        // gather first-level children
        foreach (Transform child in transform.GetComponentsInChildren<Transform>()) {
            if (child.parent == transform) {
                child.name = name + '/' + child.name;
                child.parent = transform.parent;
            }
        }
    }
}
