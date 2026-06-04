using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace GanhHangRong.Editor
{
    [InitializeOnLoad]
    public class SetupSittingAnimation
    {
        static SetupSittingAnimation()
        {
            // Tự động chạy khi script được compile xong trong Unity
            // Dùng EditorApplication.delayCall để chạy an toàn sau khi database load xong
            EditorApplication.delayCall += SetupSitting;
        }

        [MenuItem("Gánh Hàng Rong/Cấu hình animation Ngồi (Sitting)", false, 101)]
        public static void SetupSitting()
        {
            string controllerPath = "Assets/_Project/Animations/Player/PlayerAnimController.controller";
            string fbxPath = "Assets/Animations/Mixamo/Sitting Idle.fbx";
            string retargetedClipPath = "Assets/_Project/Animations/Player/Sitting_Idle_Retargeted.anim";

            // 1. Load Animator Controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogError($"[SetupSittingAnimation] Không tìm thấy Animator Controller tại: {controllerPath}");
                return;
            }

            // 2. Load Animation Clip từ file FBX
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip sourceClip = null;
            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip clip)
                {
                    // Mixamo clip thường không bắt đầu bằng "__" (như __preview__)
                    if (!clip.name.StartsWith("__"))
                    {
                        sourceClip = clip;
                        break;
                    }
                }
            }

            if (sourceClip == null)
            {
                Debug.LogError($"[SetupSittingAnimation] Không tìm thấy AnimationClip hợp lệ trong FBX: {fbxPath}");
                return;
            }

            // 3. Tiến hành Retarget clip (chuyển đổi Bone Paths và Scale) sang file .anim mới
            AnimationClip retargetedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(retargetedClipPath);
            if (retargetedClip == null)
            {
                retargetedClip = new AnimationClip();
                AssetDatabase.CreateAsset(retargetedClip, retargetedClipPath);
                Debug.Log($"[SetupSittingAnimation] Đã tạo file animation retargeted mới tại: {retargetedClipPath}");
            }
            else
            {
                retargetedClip.ClearCurves();
            }

            retargetedClip.name = "Sitting_Idle_Retargeted";

            // Xác định tỉ lệ scaling cho Hips Translation
            float scaleFactor = 1f;
            var bindings = AnimationUtility.GetCurveBindings(sourceClip);
            foreach (var binding in bindings)
            {
                if (binding.path.EndsWith("Hips") && binding.propertyName == "m_LocalPosition.y")
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                    if (curve != null && curve.keys.Length > 0)
                    {
                        float maxVal = 0f;
                        foreach (var key in curve.keys)
                        {
                            if (Mathf.Abs(key.value) > maxVal)
                                maxVal = Mathf.Abs(key.value);
                        }
                        // Nếu giá trị dịch chuyển nhỏ hơn 5m (ví dụ cao ~0.9m),
                        // tức là file FBX đang dùng hệ mét (m) trong khi model GLB dùng hệ centimet (cm)
                        if (maxVal < 5.0f)
                        {
                            scaleFactor = 100f; // Nhân 100 lần để đổi sang cm
                        }
                    }
                    break;
                }
            }

            Debug.Log($"[SetupSittingAnimation] Bắt đầu Retarget clip từ Mixamo sang GLB. Tỉ lệ scale Hips: {scaleFactor}x.");

            // Sao chép các curves và ánh xạ đường dẫn xương
            foreach (var binding in bindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                if (curve == null) continue;

                // Ánh xạ đường dẫn xương
                string mappedPath = MapPath(binding.path);
                
                var newBinding = binding;
                newBinding.path = mappedPath;

                // Nhân hệ số tỉ lệ đối với translation của Hips
                if (scaleFactor != 1f && binding.path.EndsWith("Hips") && binding.propertyName.StartsWith("m_LocalPosition"))
                {
                    Keyframe[] keys = curve.keys;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        keys[i].value *= scaleFactor;
                    }
                    curve.keys = keys;
                }

                AnimationUtility.SetEditorCurve(retargetedClip, newBinding, curve);
            }

            // Thiết lập loop cho clip để nhân vật ngồi liên tục không bị đứng dậy
            var clipSettings = AnimationUtility.GetAnimationClipSettings(retargetedClip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(retargetedClip, clipSettings);

            EditorUtility.SetDirty(retargetedClip);
            AssetDatabase.SaveAssets();

            // 4. Tìm hoặc tạo State "Sitting" trên Base Layer
            if (controller.layers.Length == 0)
            {
                Debug.LogError("[SetupSittingAnimation] Animator Controller không có layer nào!");
                return;
            }

            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;

            AnimatorState sittingState = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == "Sitting")
                {
                    sittingState = childState.state;
                    break;
                }
            }

            if (sittingState == null)
            {
                sittingState = stateMachine.AddState("Sitting");
                sittingState.writeDefaultValues = true;
                Debug.Log("[SetupSittingAnimation] Đã tạo State 'Sitting' mới.");
            }

            // Gán clip đã retarget vào state
            sittingState.motion = retargetedClip;

            // 5. Kiểm tra và thêm transition từ Any State với điều kiện State == 5
            bool transitionExists = false;
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                if (transition.destinationState == sittingState)
                {
                    bool hasCorrectCondition = false;
                    foreach (var condition in transition.conditions)
                    {
                        if (condition.parameter == "State" && 
                            condition.mode == AnimatorConditionMode.Equals && 
                            Mathf.Approximately(condition.threshold, 5f))
                        {
                            hasCorrectCondition = true;
                            break;
                        }
                    }
                    if (hasCorrectCondition)
                    {
                        transitionExists = true;
                        break;
                    }
                }
            }

            if (!transitionExists)
            {
                AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(sittingState);
                transition.duration = 0.15f;
                transition.hasExitTime = false;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, 5f, "State");
                Debug.Log("[SetupSittingAnimation] Đã thêm AnyState transition đến Sitting với điều kiện State == 5.");
            }

            // Lưu thay đổi
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SetupSittingAnimation] Cập nhật Animator Controller thành công! Đã gán clip retargeted '{retargetedClip.name}' cho trạng thái Sitting.");
        }

        private static string MapPath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
                return originalPath;

            string[] segments = originalPath.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                string name = segments[i];
                // Loại bỏ tiền tố của Mixamo
                if (name.StartsWith("mixamorig:"))
                {
                    name = name.Substring("mixamorig:".Length);
                }

                // Ánh xạ đảo ngược các đốt sống khớp với cấu trúc xương của Meshy GLB
                if (name == "Spine")
                    name = "Spine02";
                else if (name == "Spine1")
                    name = "Spine01";
                else if (name == "Spine2")
                    name = "Spine";
                else if (name == "Neck")
                    name = "neck";

                segments[i] = name;
            }

            // Thêm thư mục gốc xương của GLB model (Armature)
            return "Armature/" + string.Join("/", segments);
        }
    }
}
