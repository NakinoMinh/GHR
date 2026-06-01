using UnityEngine;

namespace GanhHangRong.Core
{
    /// <summary>
    /// Hằng số toàn game — tập trung mọi giá trị cấu hình.
    /// </summary>
    public static class Constants
    {
        // ═══════════════════════════════════════════
        // PLAYER
        // ═══════════════════════════════════════════
        public const float PLAYER_WALK_SPEED = 2.5f;
        public const float PLAYER_RUN_SPEED = 4.5f;
        public const float PLAYER_PUSH_CART_SPEED = 1.5f;
        public const float PLAYER_FATIGUE_RATE = 0.5f;          // mệt mỏi tăng/phút
        public const float PLAYER_FATIGUE_MAX = 100f;
        public const float PLAYER_FATIGUE_REST_RATE = 2f;       // hồi phục khi ngồi/phút
        public const float PLAYER_SPEED_FATIGUE_PENALTY = 0.3f; // giảm tốc khi mệt tối đa

        // STRESS
        public const float PLAYER_STRESS_MAX = 100f;
        public const float PLAYER_STRESS_RATE_SAD_CUSTOMER = 15f;   // Stress tăng khi khách bỏ đi
        public const float PLAYER_STRESS_RATE_SERVE = -5f;          // Stress giảm khi phục vụ thành công
        public const float PLAYER_STRESS_REST_RATE = 3f;            // Stress giảm khi ngồi nghỉ/phút

        // ═══════════════════════════════════════════
        // CAMERA
        // ═══════════════════════════════════════════
        public const float CAMERA_SMOOTH_TIME = 0.35f;
        public const float CAMERA_LOOK_AHEAD = 1.5f;
        public const float CAMERA_DEAD_ZONE = 0.3f;
        public const float CAMERA_Y_OFFSET = 1.5f;
        public const float CAMERA_Z_OFFSET = -15f;

        // ═══════════════════════════════════════════
        // KINH TẾ
        // ═══════════════════════════════════════════
        public const int STARTING_MONEY = 50000;           // 50,000 VNĐ
        public const int TEA_BUY_PRICE = 5000;             // giá mua trà
        public const int ICE_BUY_PRICE = 3000;             // giá mua đá
        public const int SUGAR_BUY_PRICE = 2000;           // giá mua đường
        public const int CUP_BUY_PRICE = 1000;             // giá mua ly
        public const int TRA_DA_SELL_PRICE = 5000;          // giá bán trà đá
        public const int CART_REPAIR_COST = 20000;          // chi phí sửa xe
        public const float ICE_MELT_RATE = 1f;              // % tan/phút (bình thường)
        public const float ICE_MELT_RATE_RAIN = 0.5f;       // % tan/phút (khi mưa, lạnh hơn)
        public const float ICE_MAX = 100f;

        // ═══════════════════════════════════════════
        // THỜI GIAN
        // ═══════════════════════════════════════════
        public const float GAME_MINUTES_PER_REAL_SECOND = 2f;  // 1 giây thực = 2 phút game
        public const float HOURS_IN_DAY = 24f;
        public const float DAY_START_HOUR = 5f;             // 5:00 sáng
        public const float NIGHT_START_HOUR = 20f;          // 8:00 tối
        public const float NIGHT_END_HOUR = 1f;             // 1:00 sáng

        // ═══════════════════════════════════════════
        // NPC
        // ═══════════════════════════════════════════
        public const int MAX_CONCURRENT_CUSTOMERS = 4;
        public const float NPC_WALK_SPEED_MIN = 1.5f;
        public const float NPC_WALK_SPEED_MAX = 2.5f;
        public const float NPC_PATIENCE_MIN = 30f;          // giây tối thiểu chờ
        public const float NPC_PATIENCE_MAX = 60f;          // giây tối đa chờ
        public const float NPC_DRINK_TIME_MIN = 10f;         // giây uống tối thiểu
        public const float NPC_DRINK_TIME_MAX = 25f;         // giây uống tối đa
        public const float NPC_SPAWN_INTERVAL_BASE = 15f;    // giây giữa mỗi lần spawn
        public const float NPC_TIP_CHANCE = 0.2f;            // 20% khả năng tip

        // ═══════════════════════════════════════════
        // THỜI TIẾT
        // ═══════════════════════════════════════════
        public const float WEATHER_TRANSITION_DURATION = 5f;  // giây chuyển đổi
        public const float RAIN_CUSTOMER_MODIFIER_LIGHT = 0.7f;
        public const float RAIN_CUSTOMER_MODIFIER_HEAVY = 0.3f;
        public const float WIND_CUSTOMER_MODIFIER = 0.8f;

        // ═══════════════════════════════════════════
        // EMOTIONAL FAILURE
        // ═══════════════════════════════════════════
        public const float EMOTIONAL_UPDATE_INTERVAL = 60f;  // cập nhật mỗi 60 giây game
        public const int CUSTOMERS_THRESHOLD_GOOD = 8;       // >= 8 khách = tốt
        public const int CUSTOMERS_THRESHOLD_OK = 5;         // >= 5 khách = bình thường
        public const int CUSTOMERS_THRESHOLD_BAD = 2;        // >= 2 khách = khó khăn
        public const float LIGHT_DIM_LONELY = 0.6f;          // hệ số ánh sáng khi cô đơn
        public const float LIGHT_DIM_DESPERATE = 0.35f;      // hệ số ánh sáng khi tuyệt vọng

        // ═══════════════════════════════════════════
        // DIALOGUE
        // ═══════════════════════════════════════════
        public const float TYPEWRITER_SPEED = 0.04f;         // giây/ký tự
        public const float DIALOGUE_AUTO_ADVANCE_DELAY = 2f; // giây chờ tự chuyển

        // ═══════════════════════════════════════════
        // PARALLAX
        // ═══════════════════════════════════════════
        public const float PARALLAX_SPEED_LAYER1 = 0.05f;   // biển xa
        public const float PARALLAX_SPEED_LAYER2 = 0.1f;    // thuyền & sương
        public const float PARALLAX_SPEED_LAYER3 = 0.15f;   // núi xa
        public const float PARALLAX_SPEED_LAYER4 = 0.25f;   // ánh đèn thành phố
        public const float PARALLAX_SPEED_LAYER5 = 0.5f;    // cột điện & biển hiệu

        // ═══════════════════════════════════════════
        // TAGS & LAYERS
        // ═══════════════════════════════════════════
        public const string TAG_PLAYER = "Player";
        public const string TAG_NPC = "NPC";
        public const string TAG_INTERACTABLE = "Interactable";
        public const string TAG_CUSTOMER_SEAT = "CustomerSeat";
        public const string TAG_TEA_CART = "TeaCart";

        public const string LAYER_PLAYER = "Player";
        public const string LAYER_NPC = "NPC";
        public const string LAYER_ENVIRONMENT = "Environment";
        public const string LAYER_UI = "UI";

        // ═══════════════════════════════════════════
        // SAVE
        // ═══════════════════════════════════════════
        public const string SAVE_FILE_NAME = "ganh_hang_rong_save.json";
        public const int SAVE_VERSION = 1;

        // ═══════════════════════════════════════════
        // AUDIO
        // ═══════════════════════════════════════════
        public const float AUDIO_CROSSFADE_DURATION = 2f;
        public const float AMBIENT_BASE_VOLUME = 0.5f;
        public const float MUSIC_BASE_VOLUME = 0.3f;
        public const float SFX_BASE_VOLUME = 0.7f;
    }
}
