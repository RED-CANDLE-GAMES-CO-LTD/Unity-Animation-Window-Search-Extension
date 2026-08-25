using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
#if UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
#endif

namespace JWS.Editor
{
    public class AnimationClipSearchTool : EditorWindow
    {
        private string searchQuery = "";
        private Vector2 scrollPosition;
        private List<AnimationClip> allClips = new List<AnimationClip>();
        private List<AnimationClip> filteredClips = new List<AnimationClip>();
        private System.Action<AnimationClip> onClipSelected;
        private int selectedIndex = 0;
        private bool scrollToSelected = false;
        private float scrollViewHeight = 0f;
        
        private GUIStyle searchFieldStyle;
        private GUIStyle clipButtonStyle;
        
        private const float WINDOW_WIDTH = 300f;
        private const float WINDOW_HEIGHT = 400f;
        
        [MenuItem("Window/JWS/Animation Clip Search Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationClipSearchTool>("Clip Search");
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window.position = new Rect(Screen.width / 2, Screen.height / 2, WINDOW_WIDTH, WINDOW_HEIGHT);
            window.RefreshClipList();
            FocusNextTick(window);
        }

        public static void ShowWindowWithCallback(System.Action<AnimationClip> callback)
        {
            var window = GetWindow<AnimationClipSearchTool>("Clip Search");
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window.position = new Rect(Screen.width / 2, Screen.height / 2, WINDOW_WIDTH, WINDOW_HEIGHT);
            window.onClipSelected = callback;
            window.RefreshClipList();
            FocusNextTick(window);
        }

        // Focusing immediately after GetWindow() doesn't reliably grab native OS
        // keyboard focus for a newly created floating window; deferring one tick
        // (after the native window actually exists) fixes arrow keys requiring
        // an Alt+Tab away and back before they register.
        private static void FocusNextTick(AnimationClipSearchTool window)
        {
            EditorApplication.delayCall += () =>
            {
                if (window == null) return;
#if UNITY_EDITOR_WIN
                ForceOSForegroundFocus();
#endif
                window.Focus();
            };
        }

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);

        // Windows blocks SetForegroundWindow from a background process (focus-stealing
        // prevention) unless its input queue is attached to the current foreground
        // thread's input queue. This is why the window needed an Alt+Tab away and back
        // before it would receive keyboard events.
        private static void ForceOSForegroundFocus()
        {
            IntPtr unityWindow = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (unityWindow == IntPtr.Zero) return;

            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            uint currentThreadId = GetCurrentThreadId();

            if (foregroundThreadId != currentThreadId && AttachThreadInput(foregroundThreadId, currentThreadId, true))
            {
                BringWindowToTop(unityWindow);
                SetForegroundWindow(unityWindow);
                AttachThreadInput(foregroundThreadId, currentThreadId, false);
            }
            else
            {
                BringWindowToTop(unityWindow);
                SetForegroundWindow(unityWindow);
            }
        }
#endif

        private void OnEnable()
        {
            RefreshClipList();
        }
        
        private void RefreshClipList()
        {
            allClips.Clear();
            
            // First try to get clips from current Animator Controller
            GameObject selectedGO = Selection.activeGameObject;
            bool foundControllerClips = false;
            
            if (selectedGO != null)
            {
                // Try to find Animator in the selected object or its parents
                Animator animator = FindAnimatorInHierarchy(selectedGO);
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                    if (controller != null)
                    {
                        // Get all clips from the animator controller
                        var clips = GetAllClipsFromController(controller);
                        allClips.AddRange(clips);
                        foundControllerClips = true;
                    }
                }
            }
            
            // If no controller clips found, fall back to all project clips
            if (!foundControllerClips)
            {
                // Find all animation clips in the project
                string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    
                    if (clip != null && !clip.name.StartsWith("__preview__"))
                    {
                        allClips.Add(clip);
                    }
                }
            }
            
            // Sort by name
            allClips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            
            UpdateFilteredList();
        }
        
        private Animator FindAnimatorInHierarchy(GameObject go)
        {
            if (go == null) return null;
            
            // First check the object itself
            Animator animator = go.GetComponent<Animator>();
            if (animator != null) return animator;
            
            // Then check all parents
            Transform current = go.transform.parent;
            while (current != null)
            {
                animator = current.GetComponent<Animator>();
                if (animator != null) return animator;
                current = current.parent;
            }
            
            // Finally check children (in case user selected a parent of the animator)
            animator = go.GetComponentInChildren<Animator>();
            if (animator != null) return animator;
            
            return null;
        }
        
        private List<AnimationClip> GetAllClipsFromController(AnimatorController controller)
        {
            var clips = new HashSet<AnimationClip>();
            
            // Get all states from all layers
            foreach (var layer in controller.layers)
            {
                var stateMachine = layer.stateMachine;
                if (layer.syncedLayerIndex==-1)
                    GetClipsFromStateMachine(stateMachine, clips);
                else
                    GetOverrideClipsFromStateMachine(layer, controller.layers[layer.syncedLayerIndex].stateMachine, clips);
            }
            
            return new List<AnimationClip>(clips);
        }
        
        private void GetClipsFromStateMachine(AnimatorStateMachine stateMachine, HashSet<AnimationClip> clips)
        {
            // Get clips from states
            foreach (var state in stateMachine.states)
            {
                if (state.state.motion is AnimationClip clip)
                {
                    clips.Add(clip);
                }
                else if (state.state.motion is BlendTree blendTree)
                {
                    GetClipsFromBlendTree(blendTree, clips);
                }
            }
            
            // Recursively check sub state machines
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                GetClipsFromStateMachine(subStateMachine.stateMachine, clips);
            }
        }
        
        private void GetClipsFromBlendTree(BlendTree blendTree, HashSet<AnimationClip> clips)
        {
            var children = blendTree.children;
            foreach (var child in children)
            {
                if (child.motion is AnimationClip clip)
                {
                    clips.Add(clip);
                }
                else if (child.motion is BlendTree childBlendTree)
                {
                    GetClipsFromBlendTree(childBlendTree, clips);
                }
            }
        }

        //Sync layers
        private void GetOverrideClipsFromStateMachine(AnimatorControllerLayer layer, AnimatorStateMachine stateMachine, HashSet<AnimationClip> clips)
        {
            // Get clips from states
            foreach (var state in stateMachine.states)
            {
                if (layer.GetOverrideMotion(state.state) is AnimationClip clip)
                {
                    clips.Add(clip);
                }
                else if (layer.GetOverrideMotion(state.state) is BlendTree blendTree)
                {
                    GetClipsFromBlendTree(blendTree, clips);
                }
            }

            // Recursively check sub state machines
            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                GetOverrideClipsFromStateMachine(layer, subStateMachine.stateMachine, clips);
            }
        }





        private void UpdateFilteredList()
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                filteredClips = new List<AnimationClip>(allClips);
            }
            else
            {
                string lowerQuery = searchQuery.ToLower();
                filteredClips = allClips.Where(clip =>
                    clip.name.ToLower().Contains(lowerQuery) ||
                    AssetDatabase.GetAssetPath(clip).ToLower().Contains(lowerQuery)
                ).ToList();
            }

            selectedIndex = filteredClips.Count > 0 ? Mathf.Clamp(selectedIndex, 0, filteredClips.Count - 1) : 0;
            scrollToSelected = true;
        }
        
        private void InitializeStyles()
        {
            if (searchFieldStyle == null)
            {
                searchFieldStyle = new GUIStyle("SearchTextField");
                searchFieldStyle.fontSize = 12;
                searchFieldStyle.fixedHeight = 20;
            }
            
            if (clipButtonStyle == null)
            {
                clipButtonStyle = new GUIStyle(GUI.skin.button);
                clipButtonStyle.alignment = TextAnchor.MiddleLeft;
                clipButtonStyle.fixedHeight = 18;
                clipButtonStyle.fontSize = 11;
                clipButtonStyle.padding = new RectOffset(4, 4, 2, 2);
            }
            
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            EditorGUILayout.BeginVertical();
            
            // Search field
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("SearchField");
            searchQuery = EditorGUILayout.TextField(searchQuery, searchFieldStyle, GUILayout.Height(20));
            if (EditorGUI.EndChangeCheck())
            {
                UpdateFilteredList();
            }
            
            // Auto focus on search field when window opens
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            {
                EditorGUI.FocusTextInControl("SearchField");
            }
            
            // Results info
            GameObject selectedGO = Selection.activeGameObject;
            string searchScope = "";
            if (selectedGO != null)
            {
                Animator animator = FindAnimatorInHierarchy(selectedGO);
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    searchScope = animator.runtimeAnimatorController.name;
                }
            }
            
            GUILayout.Label($"{filteredClips.Count} clips{(string.IsNullOrEmpty(searchScope) ? "" : " in " + searchScope)}", EditorStyles.miniLabel);

            // Correct the scroll position analytically, before the scroll view is laid
            // out, so the very next repaint already shows the selected item in view
            // (fixing up the rect afterwards was always one repaint too late to matter).
            if (scrollToSelected)
            {
                ScrollToShowIndex(selectedIndex);
                scrollToSelected = false;
            }

            // Clip list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < filteredClips.Count; i++)
            {
                var clip = filteredClips[i];
                bool isSelected = i == selectedIndex;

                Color previousColor = GUI.backgroundColor;
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.24f, 0.48f, 0.90f);
                }

                if (GUILayout.Button(clip.name, clipButtonStyle, GUILayout.Height(18)))
                {
                    selectedIndex = i;
                    ApplyToAnimationWindow(clip);
                }

                GUI.backgroundColor = previousColor;
            }

            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
            {
                scrollViewHeight = GUILayoutUtility.GetLastRect().height;
            }

            EditorGUILayout.EndVertical();

            // Handle keyboard navigation
            HandleKeyboardNavigation();
        }
        
        private void HandleKeyboardNavigation()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    if (filteredClips.Count > 0)
                    {
                        selectedIndex = Mathf.Max(0, selectedIndex - 1);
                        scrollToSelected = true;
                        Repaint();
                    }
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    if (filteredClips.Count > 0)
                    {
                        selectedIndex = Mathf.Min(filteredClips.Count - 1, selectedIndex + 1);
                        scrollToSelected = true;
                        Repaint();
                    }
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (filteredClips.Count > 0)
                    {
                        int indexToApply = Mathf.Clamp(selectedIndex, 0, filteredClips.Count - 1);
                        ApplyToAnimationWindow(filteredClips[indexToApply]);
                        Event.current.Use();
                    }
                }
            }
        }

        private const float ITEM_ROW_HEIGHT = 20f; // 18px button + ~2px auto-layout spacing

        private void ScrollToShowIndex(int index)
        {
            if (scrollViewHeight <= 0f) return;

            float itemY = index * ITEM_ROW_HEIGHT;
            float itemBottom = itemY + ITEM_ROW_HEIGHT;

            if (itemY < scrollPosition.y)
            {
                scrollPosition.y = itemY;
            }
            else if (itemBottom > scrollPosition.y + scrollViewHeight)
            {
                scrollPosition.y = itemBottom - scrollViewHeight;
            }
        }
        
        private void ApplyToAnimationWindow(AnimationClip clip)
        {
            // Get the Animation window
            System.Type animationWindowType = System.Type.GetType("UnityEditor.AnimationWindow, UnityEditor");
            if (animationWindowType != null)
            {
                EditorWindow animWindow = EditorWindow.GetWindow(animationWindowType);
                if (animWindow != null)
                {
                    // Focus the window first
                    animWindow.Focus();
                    
                    // Get AnimationWindowState
                    var stateProperty = animationWindowType.GetProperty("state", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (stateProperty != null)
                    {
                        var state = stateProperty.GetValue(animWindow);
                        if (state != null)
                        {
                            var stateType = state.GetType();
                            
                            // Try to set activeAnimationClip
                            var activeClipProperty = stateType.GetProperty("activeAnimationClip", 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                            
                            if (activeClipProperty != null && activeClipProperty.CanWrite)
                            {
                                activeClipProperty.SetValue(state, clip, null);
                                
                                // Force refresh
                                var refreshMethod = stateType.GetMethod("ForceRefresh", 
                                    System.Reflection.BindingFlags.Public | 
                                    System.Reflection.BindingFlags.NonPublic | 
                                    System.Reflection.BindingFlags.Instance);
                                
                                if (refreshMethod != null)
                                {
                                    refreshMethod.Invoke(state, null);
                                }
                                
                                animWindow.Repaint();
                                Debug.Log($"Successfully switched to animation clip: {clip.name}");
                                
                                // Close search window after applying
                                Close();
                                return;
                            }
                        }
                    }
                    
                    // Alternative approach: Use reflection to access internal methods
                    var methods = animationWindowType.GetMethods(
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    foreach (var method in methods)
                    {
                        // Look for methods that might switch clips
                        if (method.Name.Contains("SwitchClip") || 
                            method.Name.Contains("ChangeClip") || 
                            method.Name.Contains("SetClip"))
                        {
                            var parameters = method.GetParameters();
                            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(AnimationClip))
                            {
                                method.Invoke(animWindow, new object[] { clip });
                                animWindow.Repaint();
                                Debug.Log($"Applied animation clip using {method.Name}: {clip.name}");
                                Close();
                                return;
                            }
                        }
                    }
                    
                    // Last resort: Select the clip and inform user
                    Selection.activeObject = clip;
                    EditorGUIUtility.PingObject(clip);
                    Debug.LogWarning($"Could not automatically switch to clip '{clip.name}'. Please select it manually from the Animation window dropdown.");
                }
            }
        }
        
        private void OnLostFocus()
        {
            // Keep window open when losing focus
        }
    }
    
    // Context menu integration
    public static class AnimationClipSearchContextMenu
    {
        [MenuItem("Assets/Search Animation Clips", false, 2000)]
        private static void SearchAnimationClips()
        {
            AnimationClipSearchTool.ShowWindow();
        }
        
        [MenuItem("Assets/Search Animation Clips", true)]
        private static bool SearchAnimationClipsValidation()
        {
            return true;
        }
    }
}