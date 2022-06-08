using System.Threading.Tasks;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine;

public static class Utils
{
    private static TaskCompletionSource<bool> pauseSource;
    private static Task Pause => pauseSource == null ? Task.CompletedTask : pauseSource.Task;

    public static void PauseTasks() => pauseSource = new TaskCompletionSource<bool>();
    public static void ResumeTasks() => pauseSource.SetResult(true);
    
    public static float GetAverageX(MonoBehaviour[] objects) => GetAverageCoord(objects, 0);
    public static float GetAverageY(MonoBehaviour[] objects) => GetAverageCoord(objects, 1);
    public static float GetAverageZ(MonoBehaviour[] objects) => GetAverageCoord(objects, 2);
    private static float GetAverageCoord(MonoBehaviour[] objects, int coordIndex)
    {
        float averageCoord = 0;
        foreach (var obj in objects) averageCoord += obj.transform.position[coordIndex];
        averageCoord /= objects.Length;

        return averageCoord;
    }

    public static async Task MoveTransformTo(Transform transform, Transform destination, float moveSpeed, float angularSpeed)
    {
        while (Application.isPlaying && (transform.position != destination.position || transform.rotation != destination.rotation))
        {
            transform.position = Vector3.MoveTowards(transform.position, destination.position, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, destination.rotation, angularSpeed * Time.deltaTime);

            await Pause;
            await Task.Yield();
        }
    }

    /// <summary>
    /// Spawns in an object at <paramref name="position"/>, and fades it out over <paramref name="duration"/> seconds
    /// </summary>
    public static async void FadeObject(GameObject objectPrefab, Vector3 position, float moveSpeed, float duration)
    {
        var numTransform = Object.Instantiate(objectPrefab, position, Quaternion.identity, null).transform;
        var meshes = numTransform.GetComponentsInChildren<MeshRenderer>();

        var deltaPos = moveSpeed * Time.deltaTime * Vector3.up;
        var startColor = meshes[0].material.color;
        var endColor = new Color(startColor.r, startColor.g, startColor.b, 0);
        var time = 0f;
        while (time < duration)
        {
            await Pause;
            await Task.Yield();
            if (numTransform == null) break;

            numTransform.position += deltaPos;
            var newColor = Color.Lerp(startColor, endColor, time / duration);
            foreach (var mesh in meshes) mesh.material.color = newColor;

            time += Time.deltaTime;
        }

        if (numTransform) Object.Destroy(numTransform.gameObject);
    }

    /// <summary>
    /// Moves transform and all its children to given layer
    /// </summary>
    /// <param name="root">Starting object for layer transfer</param>
    /// <param name="layer">Selected layer (LayerMask.NameToLayer is recommended)</param>
    public static void MoveToLayer(Transform root, int layer)
    {
        Stack<Transform> moveTargets = new Stack<Transform>();
        moveTargets.Push(root);

        Transform currentTarget;
        while (moveTargets.Count > 0)
        {
            currentTarget = moveTargets.Pop();
            currentTarget.gameObject.layer = layer;
            foreach (Transform child in currentTarget) moveTargets.Push(child);
        }
    }

    private static readonly KeyControl[] numberKeys = new KeyControl[] { Keyboard.current.digit0Key, Keyboard.current.digit1Key, Keyboard.current.digit2Key, Keyboard.current.digit3Key, Keyboard.current.digit4Key, Keyboard.current.digit5Key, Keyboard.current.digit6Key, Keyboard.current.digit7Key, Keyboard.current.digit8Key, Keyboard.current.digit9Key };
    public static void AddNumbersThisFrame(ref StringBuilder sb)
    {
        foreach (KeyControl key in numberKeys) if (key.wasPressedThisFrame) sb.Append(key.name);
    }
}
