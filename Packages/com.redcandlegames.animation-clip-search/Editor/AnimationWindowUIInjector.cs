using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedCandleGames.Editor
{
    [InitializeOnLoad]
    public static class AnimationWindowUIInjector
    {
        private static EditorWindow lastAnimationWindow;
        private static VisualElement injectedSearchContainer;
        private static VisualElement searchDropdown;
        private static TextField searchField;
        private static bool isInjected = false;
        private static List<AnimationClip> allClips = new List<AnimationClip>();
        private static List<AnimationClip> filteredClips = new List<AnimationClip>();
        
        static AnimationWindowUIInjector()
        {
            EditorApplication.update += OnEditorUpdate;
        }
        
        private static void OnEditorUpdate()
        {
            var animationWindow = GetAnimationWindow();
            
            if (animationWindow != null)
            {
                if (animationWindow != lastAnimationWindow || !isInjected)
                {
                    // Delay injection to ensure window is fully initialized
                    EditorApplication.delayCall += () =>
                    {
                        if (animationWindow != null)
                        {
                            InjectSearchUI(animationWindow);
                            lastAnimationWindow = animationWindow;
                        }
                    };
                }
            }
            else
            {
                if (isInjected)
                {
                    CleanupInjection();
                }
            }
        }
        
        private static EditorWindow GetAnimationWindow()
        {
            var assembly = typeof(EditorWindow).Assembly;
            var animationWindowType = assembly.GetType("UnityEditor.AnimationWindow");
            
            if (animationWindowType != null)
            {
                var windows = Resources.FindObjectsOfTypeAll(animationWindowType);
                if (windows.Length > 0)
                {
                    return windows[0] as EditorWindow;
                }
            }
            
            return null;
        }
        
        private static void InjectSearchUI(EditorWindow animWindow)
        {
            try
            {
                var rootVisualElement = animWindow.rootVisualElement;
                
                if (rootVisualElement == null)
                {
                    Debug.LogWarning("AnimationWindow rootVisualElement is null. This might happen in older Unity versions or when the window is not fully initialized.");
                    return;
                }
                
                // Check if already injected
                if (injectedSearchContainer != null && rootVisualElement.Contains(injectedSearchContainer))
                {
                    return;
                }
                
                // Log Unity version for debugging
                Debug.Log($"Attempting to inject search UI into Animation Window (Unity {Application.unityVersion})");
                
                // Create search container
                injectedSearchContainer = new VisualElement();
                injectedSearchContainer.name = "AnimationClipSearchContainer";
                injectedSearchContainer.style.flexDirection = FlexDirection.Row;
                injectedSearchContainer.style.alignItems = Align.Center;
                injectedSearchContainer.style.height = 25;
                injectedSearchContainer.style.paddingLeft = 5;
                injectedSearchContainer.style.paddingRight = 5;
                injectedSearchContainer.style.paddingTop = 2;
                injectedSearchContainer.style.paddingBottom = 2;
                injectedSearchContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
                injectedSearchContainer.style.borderBottomWidth = 1;
                injectedSearchContainer.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                
                // Create search icon/label
                var searchLabel = new Label("🔍");
                searchLabel.style.marginRight = 5;
                searchLabel.style.fontSize = 14;
                injectedSearchContainer.Add(searchLabel);
                
                // Create search field
                searchField = new TextField();
                searchField.name = "AnimationClipSearchField";
                searchField.style.flexGrow = 1;
                searchField.style.height = 20;
                
                // Try to set placeholder using reflection (safer for different Unity versions)
                TrySetPlaceholder(searchField, "Search animation clips (Alt+S)");
                
                // Register value changed callback
                searchField.RegisterValueChangedCallback(OnSearchValueChanged);
                
                // Register keyboard events
                searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
                
                injectedSearchContainer.Add(searchField);
                
                // Create clear button
                var clearButton = new Button(ClearSearch);
                clearButton.text = "✕";
                clearButton.style.width = 20;
                clearButton.style.height = 20;
                clearButton.style.marginLeft = 5;
                clearButton.style.display = DisplayStyle.None;
                injectedSearchContainer.Add(clearButton);
                
                // Find the best place to insert our search UI
                // Look for the toolbar or first child element
                VisualElement insertTarget = null;
                VisualElement insertAfter = null;
                
                // Try to find the toolbar
                var toolbar = rootVisualElement.Q("AnimationWindowToolbar") ?? 
                             rootVisualElement.Q("Toolbar") ??
                             rootVisualElement.Q(className: "unity-toolbar");
                
                if (toolbar != null)
                {
                    // Insert after toolbar
                    insertTarget = toolbar.parent;
                    insertAfter = toolbar;
                }
                else
                {
                    // Insert at the top
                    insertTarget = rootVisualElement;
                }
                
                if (insertTarget != null)
                {
                    if (insertAfter != null)
                    {
                        var index = insertTarget.IndexOf(insertAfter);
                        if (index >= 0)
                        {
                            insertTarget.Insert(index + 1, injectedSearchContainer);
                        }
                        else
                        {
                            insertTarget.Insert(0, injectedSearchContainer);
                        }
                    }
                    else
                    {
                        insertTarget.Insert(0, injectedSearchContainer);
                    }
                }
                else
                {
                    // Fallback
                    rootVisualElement.Insert(0, injectedSearchContainer);
                }
                
                // Create dropdown container (initially hidden)
                searchDropdown = new VisualElement();
                searchDropdown.name = "AnimationClipSearchDropdown";
                searchDropdown.style.position = Position.Absolute;
                searchDropdown.style.top = 27;  // Fixed position below search container
                searchDropdown.style.left = 5;
                searchDropdown.style.right = 5;
                searchDropdown.style.maxHeight = 200;
                searchDropdown.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.95f);
                searchDropdown.style.borderBottomLeftRadius = 3;
                searchDropdown.style.borderBottomRightRadius = 3;
                searchDropdown.style.borderLeftWidth = 1;
                searchDropdown.style.borderRightWidth = 1;
                searchDropdown.style.borderBottomWidth = 1;
                searchDropdown.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                searchDropdown.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                searchDropdown.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                searchDropdown.style.display = DisplayStyle.None;
                searchDropdown.style.zIndex = 1000;  // Ensure dropdown appears above other content
                
                // Create scroll view for dropdown
                var scrollView = new ScrollView();
                scrollView.style.maxHeight = 200;
                searchDropdown.Add(scrollView);
                
                rootVisualElement.Add(searchDropdown);
                
                // Refresh clip list
                RefreshClipList();
                
                isInjected = true;
                Debug.Log($"Successfully injected search UI into Animation Window (Unity {Application.unityVersion})");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to inject search UI into Animation Window: {e.Message}");
                Debug.LogError($"Unity Version: {Application.unityVersion}");
                Debug.LogError($"Stack Trace: {e.StackTrace}");
                
                // Log additional diagnostic info
                if (animWindow != null)
                {
                    Debug.LogError($"Window Type: {animWindow.GetType().FullName}");
                    Debug.LogError($"Has rootVisualElement: {animWindow.rootVisualElement != null}");
                }
                
                isInjected = false;
            }
        }
        
        private static void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            var searchText = evt.newValue;
            
            // Update clear button visibility
            var clearButton = injectedSearchContainer?.Q<Button>();
            if (clearButton != null)
            {
                clearButton.style.display = string.IsNullOrEmpty(searchText) 
                    ? DisplayStyle.None 
                    : DisplayStyle.Flex;
            }
            
            // Update filtered clips and dropdown
            UpdateFilteredClips(searchText);
            UpdateDropdown();
        }
        
        private static void UpdateFilteredClips(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                filteredClips.Clear();
                return;
            }
            
            string lowerQuery = searchText.ToLower();
            filteredClips = allClips.Where(clip => 
                clip.name.ToLower().Contains(lowerQuery) ||
                AssetDatabase.GetAssetPath(clip).ToLower().Contains(lowerQuery)
            ).Take(10).ToList(); // Limit to 10 results for better UI
        }
        
        private static void UpdateDropdown()
        {
            if (searchDropdown == null) return;
            
            var scrollView = searchDropdown.Q<ScrollView>();
            if (scrollView == null) return;
            
            // Clear existing items
            scrollView.Clear();
            
            if (filteredClips.Count == 0 || string.IsNullOrEmpty(searchField?.value))
            {
                searchDropdown.style.display = DisplayStyle.None;
                return;
            }
            
            // Show dropdown
            searchDropdown.style.display = DisplayStyle.Flex;
            
            // Add filtered clips
            foreach (var clip in filteredClips)
            {
                var button = new Button(() => SelectClip(clip));
                button.text = clip.name;
                button.style.height = 20;
                button.style.justifyContent = Justify.FlexStart;
                button.style.paddingLeft = 10;
                button.style.paddingRight = 10;
                button.style.marginBottom = 1;
                button.style.backgroundColor = new Color(0, 0, 0, 0);
                
                // Add hover effect
                button.RegisterCallback<MouseEnterEvent>(e => 
                    button.style.backgroundColor = new Color(0.3f, 0.5f, 0.7f, 0.3f));
                button.RegisterCallback<MouseLeaveEvent>(e => 
                    button.style.backgroundColor = new Color(0, 0, 0, 0));
                
                scrollView.Add(button);
            }
        }
        
        private static void SelectClip(AnimationClip clip)
        {
            if (clip == null) return;
            
            // Apply clip to animation window
            ApplyClipToAnimationWindow(clip);
            
            // Clear search and hide dropdown
            ClearSearch();
            searchDropdown.style.display = DisplayStyle.None;
        }
        
        private static void ApplyClipToAnimationWindow(AnimationClip clip)
        {
            var animWindow = GetAnimationWindow();
            if (animWindow == null) return;
            
            try
            {
                // Get AnimationWindowState
                var animWindowType = animWindow.GetType();
                var stateProperty = animWindowType.GetProperty("state", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (stateProperty != null)
                {
                    var state = stateProperty.GetValue(animWindow);
                    if (state != null)
                    {
                        var stateType = state.GetType();
                        
                        // Try to set activeAnimationClip
                        var activeClipProperty = stateType.GetProperty("activeAnimationClip", 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (activeClipProperty != null && activeClipProperty.CanWrite)
                        {
                            activeClipProperty.SetValue(state, clip, null);
                            
                            // Force refresh
                            var refreshMethod = stateType.GetMethod("ForceRefresh", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (refreshMethod != null)
                            {
                                refreshMethod.Invoke(state, null);
                            }
                            
                            animWindow.Repaint();
                            Debug.Log($"Successfully switched to animation clip: {clip.name}");
                            return;
                        }
                    }
                }
                
                // Fallback: select and ping
                Selection.activeObject = clip;
                EditorGUIUtility.PingObject(clip);
                Debug.LogWarning($"Could not automatically switch clip. Selected: {clip.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to apply clip: {e.Message}");
            }
        }
        
        private static void OnSearchKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                // Select first result if available
                if (filteredClips.Count > 0)
                {
                    SelectClip(filteredClips[0]);
                }
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                // Clear search
                ClearSearch();
                evt.StopPropagation();
            }
        }
        
        private static void ClearSearch()
        {
            if (searchField != null)
            {
                searchField.value = "";
                searchField.Focus();
            }
            
            if (searchDropdown != null)
            {
                searchDropdown.style.display = DisplayStyle.None;
            }
        }
        
        private static void RefreshClipList()
        {
            allClips.Clear();
            
            // First try to get clips from current Animator Controller
            GameObject selectedGO = Selection.activeGameObject;
            bool foundControllerClips = false;
            
            if (selectedGO != null)
            {
                // Try to find Animator in the selected object or its hierarchy
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
            
            // Remove duplicates and sort by name
            var uniqueClips = new HashSet<AnimationClip>(allClips);
            allClips = new List<AnimationClip>(uniqueClips);
            allClips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        }
        
        private static Animator FindAnimatorInHierarchy(GameObject go)
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
            
            // Finally check children
            animator = go.GetComponentInChildren<Animator>();
            if (animator != null) return animator;
            
            return null;
        }
        
        private static List<AnimationClip> GetAllClipsFromController(AnimatorController controller)
        {
            var clips = new HashSet<AnimationClip>();
            
            // Get all states from all layers
            foreach (var layer in controller.layers)
            {
                var stateMachine = layer.stateMachine;
                GetClipsFromStateMachine(stateMachine, clips);
            }
            
            return new List<AnimationClip>(clips);
        }
        
        private static void GetClipsFromStateMachine(AnimatorStateMachine stateMachine, HashSet<AnimationClip> clips)
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
        
        private static void GetClipsFromBlendTree(BlendTree blendTree, HashSet<AnimationClip> clips)
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
        
        private static void TrySetPlaceholder(TextField textField, string placeholderText)
        {
            try
            {
                // Use reflection to check if placeholder property exists
                var placeholderProperty = typeof(TextField).GetProperty("placeholder");
                if (placeholderProperty != null && placeholderProperty.CanWrite)
                {
                    placeholderProperty.SetValue(textField, placeholderText);
                }
                else
                {
                    // Fallback: add a label hint that shows when field is empty
                    var placeholderLabel = new Label(placeholderText);
                    placeholderLabel.style.position = Position.Absolute;
                    placeholderLabel.style.left = 5;
                    placeholderLabel.style.top = 0;
                    placeholderLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    placeholderLabel.style.fontSize = 11;
                    placeholderLabel.pickingMode = PickingMode.Ignore;
                    
                    textField.Add(placeholderLabel);
                    
                    // Hide/show based on text content
                    textField.RegisterValueChangedCallback(evt => 
                    {
                        placeholderLabel.style.display = string.IsNullOrEmpty(evt.newValue) 
                            ? DisplayStyle.Flex 
                            : DisplayStyle.None;
                    });
                }
            }
            catch (Exception e)
            {
                // Silently fail - placeholder is not critical
                Debug.LogWarning($"Could not set placeholder text: {e.Message}");
            }
        }
        
        private static void CleanupInjection()
        {
            if (injectedSearchContainer != null && injectedSearchContainer.parent != null)
            {
                injectedSearchContainer.parent.Remove(injectedSearchContainer);
            }
            
            if (searchDropdown != null && searchDropdown.parent != null)
            {
                searchDropdown.parent.Remove(searchDropdown);
            }
            
            injectedSearchContainer = null;
            searchDropdown = null;
            searchField = null;
            isInjected = false;
            lastAnimationWindow = null;
            allClips.Clear();
            filteredClips.Clear();
        }
        
        // Test menu item to manually trigger injection
        [MenuItem("Window/Animation/Test UI Injection")]
        private static void TestUIInjection()
        {
            var animWindow = GetAnimationWindow();
            if (animWindow != null)
            {
                InjectSearchUI(animWindow);
            }
            else
            {
                Debug.LogWarning("Animation Window is not open");
            }
        }
    }
}