using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.UI;
using UnityEditor;
using UnityEngine.Events;

[RequireComponent(typeof(UserHeightUtility))]
public class AvatarSwapper : MonoBehaviour
{
    public int activeAvatar = 0;
    private int chosenAvatar;
    public HVRManager manager;
    public HVRInputModule uiInputModule;
    public UserHeightUtility userHeightUtility;
    public AvatarNetworkedComponents avatarNetworkedComponents;
    public List<GameObject> avatars = new List<GameObject>();
    public GameObject spawnedAvatar { get; private set; }

    private AvatarComponents currentAvatarComponents;

    public UnityEvent<int> OnAvatarSwapped;

    void Awake()
    {
        if(!userHeightUtility)
            userHeightUtility = GetComponent<UserHeightUtility>();

        if(!avatarNetworkedComponents)
            avatarNetworkedComponents = GetComponent<AvatarNetworkedComponents>();

        SwapAvatar(activeAvatar);
    }

    // Stores values given by UI elements
    public void ChooseAvatar(int index)
    {
        chosenAvatar = index;
    }

    // Create the chosen avatar and remove the current avatar
    public void SwapAvatar(int index)
    {
        if(index >= 0 && index < avatars.Count)
        {
            currentAvatarComponents?.RemoveComponents();
            Destroy(spawnedAvatar);

            spawnedAvatar = Instantiate(original: avatars[index],
                                        parent: gameObject.transform,
                                        position: transform.position,
                                        rotation: transform.rotation);
            activeAvatar = index;

            currentAvatarComponents = spawnedAvatar.GetComponent<AvatarComponents>();
            avatarNetworkedComponents?.DeepCopy(spawnedAvatar.GetComponent<AvatarNetworkedComponents>());
            currentAvatarComponents.SetHVRComponents(manager, uiInputModule);
            currentAvatarComponents.SetUserHeightUtility(userHeightUtility);

            userHeightUtility.floor = currentAvatarComponents.floor;
            userHeightUtility.camera = currentAvatarComponents.camera;

            OnAvatarSwapped.Invoke(activeAvatar);
        }
        else
        {
            Debug.LogError("Chosen avatar index out of bounds.");
        }
    }

    // Method to be called from UI elements without parameters
    public void SwapToChosenAvatar()
    {
        SwapAvatar(chosenAvatar);
    }
}

[CustomEditor(typeof(AvatarSwapper))]
public class AvatarSwapperEditor : Editor
{
    private int avatarIndex;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Application.isPlaying)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

            avatarIndex = EditorGUILayout.IntField(label: "Avatar Index", value: avatarIndex);

            if (GUILayout.Button("Swap"))
                (target as AvatarSwapper).SwapAvatar(avatarIndex);
        }
            
    }
}