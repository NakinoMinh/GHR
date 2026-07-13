#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace GanhHangRong.Editor
{
    /// <summary>
    /// Thiết lập Animator Controller tự động cho Player để phát animation cầm ly nước.
    /// Tự động remap bone paths từ Mixamo (mixamorig:X) sang tên bone của model Meshy AI (X).
    /// Đồng thời tự động gán mô hình ly nước fbx vào các CartItem trong scene.
    /// </summary>
    public class AnimatorSetup
    {
        [MenuItem("GHR/Thiết lập Animator & Mô hình")]
        public static void SetupPlayerAnimator()
        {
            string controllerPath = "Assets/_Project/Animations/Player/PlayerAnimController.controller";
            string animFbxPath = "Assets/Animations/holdLy.fbx";
            string cupFbxPath = "Assets/_Project/Resources/lytrada/Meshy_AI_Cold_beer_in_a_glass__0604062641_texture.fbx";
            string remappedClipPath = "Assets/_Project/Animations/Player/HoldCup_Remapped.anim";

            // 1. Tự động tìm và gán mô hình ly trà đá fbx vào CartItem trong Scene
            GameObject cupAsset = AssetDatabase.LoadAssetAtPath<GameObject>(cupFbxPath);
            if (cupAsset != null)
            {
                var cartItems = Object.FindObjectsByType<Interaction.CartItem>(FindObjectsSortMode.None);
                foreach (var item in cartItems)
                {
                    SerializedObject so = new SerializedObject(item);
                    var prop = so.FindProperty("teaCupHeldPrefab");
                    if (prop != null && prop.objectReferenceValue != cupAsset)
                    {
                        prop.objectReferenceValue = cupAsset;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(item.gameObject);
                        Debug.Log($"[AnimatorSetup] Đã gán mô hình ly trà đá fbx vào {item.name}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[AnimatorSetup] Không tìm thấy mô hình ly trà đá fbx tại {cupFbxPath}");
            }

            // 2. Thiết lập Animator Controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"[AnimatorSetup] Không tìm thấy Animator Controller tại {controllerPath}");
                return;
            }

            // Tìm clip animation cầm ly trong cammottay.fbx
            AnimationClip sourceClip = null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(animFbxPath);
            foreach (var asset in subAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    sourceClip = clip;
                    break;
                }
            }

            if (sourceClip == null)
            {
                Debug.LogWarning($"[AnimatorSetup] Không tìm thấy AnimationClip nào trong {animFbxPath}");
                return;
            }

            // 3. Tạo bản sao của clip với bone paths đã remap (xóa prefix mixamorig:)
            AnimationClip remappedClip = CreateRemappedClip(sourceClip, remappedClipPath);
            if (remappedClip == null)
            {
                Debug.LogWarning("[AnimatorSetup] Không thể tạo remapped clip");
                return;
            }

            // 4. Thêm Parameter "IsHoldingCup" nếu chưa có
            bool hasParam = false;
            foreach (var param in controller.parameters)
            {
                if (param.name == "IsHoldingCup")
                {
                    hasParam = true;
                    break;
                }
            }
            if (!hasParam)
            {
                controller.AddParameter("IsHoldingCup", AnimatorControllerParameterType.Bool);
            }

            // 5. Tìm hoặc tạo Layer "HoldCupLayer"
            AnimatorControllerLayer holdLayer = null;
            int holdLayerIndex = -1;
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == "HoldCupLayer")
                {
                    holdLayer = controller.layers[i];
                    holdLayerIndex = i;
                    break;
                }
            }

            // Tạo Avatar Mask với Transform paths (phù hợp cho Generic rig)
            if (holdLayer == null)
            {
                holdLayer = new AnimatorControllerLayer
                {
                    name = "HoldCupLayer",
                    avatarMask = null, // Không dùng Humanoid mask, dùng null = ảnh hưởng toàn bộ
                    defaultWeight = 0f,
                    stateMachine = new AnimatorStateMachine()
                };
                holdLayer.stateMachine.name = holdLayer.name;
                AssetDatabase.AddObjectToAsset(holdLayer.stateMachine, controller);
                
                controller.AddLayer(holdLayer);
                AssetDatabase.SaveAssets();

                holdLayerIndex = controller.layers.Length - 1;
                holdLayer = controller.layers[holdLayerIndex];
            }

            // 6. Kiểm tra nếu đã có đủ state thì chỉ cập nhật motion, tránh xóa sub-asset gây Broken PPtr
            var stateMachine = holdLayer.stateMachine;
            bool hasEmpty = false;
            bool hasHoldCup = false;
            foreach (var s in stateMachine.states)
            {
                if (s.state != null && s.state.name == "Empty") hasEmpty = true;
                if (s.state != null && s.state.name == "HoldCup")
                {
                    s.state.motion = remappedClip;
                    hasHoldCup = true;
                }
            }

            if (!hasEmpty || !hasHoldCup)
            {
                var states = stateMachine.states;
                for (int i = states.Length - 1; i >= 0; i--)
                {
                    if (states[i].state != null)
                    {
                        var st = states[i].state;
                        stateMachine.RemoveState(st);
                        Undo.DestroyObjectImmediate(st);
                    }
                }

                AnimatorState emptyState = stateMachine.AddState("Empty", new Vector3(200, 0, 0));
                AnimatorState holdCupState = stateMachine.AddState("HoldCup", new Vector3(200, 120, 0));
                holdCupState.motion = remappedClip;

                stateMachine.defaultState = emptyState;

                var transToHold = emptyState.AddTransition(holdCupState);
                transToHold.AddCondition(AnimatorConditionMode.If, 0, "IsHoldingCup");
                transToHold.duration = 0.15f;
                transToHold.hasExitTime = false;

                var transToEmpty = holdCupState.AddTransition(emptyState);
                transToEmpty.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHoldingCup");
                transToEmpty.duration = 0.15f;
                transToEmpty.hasExitTime = false;
            }

            // Đảm bảo Blending Mode là Override và xóa mask (null = toàn bộ body)
            var layers = controller.layers;
            layers[holdLayerIndex].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[holdLayerIndex].avatarMask = null; // Bỏ Humanoid mask, cho phép toàn bộ bones
            controller.layers = layers;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimatorSetup] ✅ Hoàn tất cấu hình HoldCupLayer với clip remapped từ {sourceClip.name}");
        }

        /// <summary>
        /// Tạo bản sao của AnimationClip với bone paths đã được remap.
        /// Xóa prefix "mixamorig:" khỏi tất cả curve paths.
        /// </summary>
        private static AnimationClip CreateRemappedClip(AnimationClip source, string savePath)
        {
            // Kiểm tra xem đã có clip remapped chưa
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
            
            AnimationClip newClip = existing != null ? existing : new AnimationClip();
            newClip.name = "HoldCup_Remapped";

            // Xóa toàn bộ curves cũ
            newClip.ClearCurves();

            // Copy và remap EditorCurveBindings (chỉ lấy phần tay phải cầm ly: RightShoulder, RightArm, RightForeArm, RightHand và ngón tay)
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
            foreach (var binding in bindings)
            {
                if (!binding.path.Contains("RightShoulder") && !binding.path.Contains("RightArm") && !binding.path.Contains("RightForeArm") && !binding.path.Contains("RightHand"))
                {
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                string remappedPath = RemapBonePath(binding.path);
                
                // Chỉnh lại góc xoay chuẩn trên Meshy AI rig cho 3 xương tay chính để lòng bàn tay hướng vào ly thay vì bị ngửa phẳng ra
                if (binding.propertyName.StartsWith("m_LocalRotation"))
                {
                    Quaternion targetQ = Quaternion.identity;
                    bool overrideRot = false;
                    if (remappedPath.EndsWith("RightArm")) { targetQ = Quaternion.Euler(85.24f, 88.36f, 98.37f); overrideRot = true; }
                    else if (remappedPath.EndsWith("RightForeArm")) { targetQ = Quaternion.Euler(6.48f, 12.85f, 87.18f); overrideRot = true; }
                    else if (remappedPath.EndsWith("RightHand")) { targetQ = Quaternion.Euler(285.62f, 158.36f, 206.57f); overrideRot = true; }

                    if (overrideRot)
                    {
                        float val = 0f;
                        if (binding.propertyName == "m_LocalRotation.x") val = targetQ.x;
                        else if (binding.propertyName == "m_LocalRotation.y") val = targetQ.y;
                        else if (binding.propertyName == "m_LocalRotation.z") val = targetQ.z;
                        else if (binding.propertyName == "m_LocalRotation.w") val = targetQ.w;
                        curve = AnimationCurve.Constant(0f, source.length, val);
                    }
                }

                EditorCurveBinding newBinding = new EditorCurveBinding
                {
                    path = remappedPath,
                    propertyName = binding.propertyName,
                    type = binding.type
                };

                AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
            }

            // Copy ObjectReference curves (nếu có)
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            foreach (var binding in objectBindings)
            {
                if (!binding.path.Contains("RightShoulder") && !binding.path.Contains("RightArm") && !binding.path.Contains("RightForeArm") && !binding.path.Contains("RightHand"))
                {
                    continue;
                }

                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(source, binding);
                string remappedPath = RemapBonePath(binding.path);
                
                EditorCurveBinding newBinding = new EditorCurveBinding
                {
                    path = remappedPath,
                    propertyName = binding.propertyName,
                    type = binding.type
                };

                AnimationUtility.SetObjectReferenceCurve(newClip, newBinding, keyframes);
            }

            // Cài đặt loop
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(newClip, settings);

            // Lưu clip
            if (existing == null)
            {
                AssetDatabase.CreateAsset(newClip, savePath);
            }
            else
            {
                EditorUtility.SetDirty(newClip);
            }
            AssetDatabase.SaveAssets();

            Debug.Log($"[AnimatorSetup] Đã tạo/cập nhật remapped clip tại {savePath} với {bindings.Length} curves");
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
        }

        /// <summary>
        /// Remap bone path: xóa prefix "mixamorig:" khỏi mỗi segment.
        /// Ví dụ: "Armature/mixamorig:Hips/mixamorig:Spine/mixamorig:RightArm" 
        /// → "Armature/Hips/Spine/RightArm"
        /// 
        /// Cũng xử lý mapping đặc biệt:
        /// - "Spine1" → "Spine01", "Spine2" → "Spine02" (nếu model dùng naming khác)
        /// </summary>
        private static string RemapBonePath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath)) return originalPath;

            string[] segments = originalPath.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                // Xóa prefix "mixamorig:"
                if (segments[i].StartsWith("mixamorig:"))
                {
                    segments[i] = segments[i].Substring("mixamorig:".Length);
                }

                // Mapping tên bone đặc biệt (Mixamo → Meshy AI)
                switch (segments[i])
                {
                    case "Spine1":
                        segments[i] = "Spine01";
                        break;
                    case "Spine2":
                        segments[i] = "Spine02";
                        break;
                }
            }

            return string.Join("/", segments);
        }
    }
}
#endif
