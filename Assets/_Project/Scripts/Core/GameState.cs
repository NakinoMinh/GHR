namespace GanhHangRong.Core
{
    /// <summary>
    /// Trạng thái tổng thể của game.
    /// </summary>
    public enum GamePhase
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        Dialogue,
        Cutscene,
        DayTransition,
        GameOver
    }

    /// <summary>
    /// Pha trong ngày — quyết định gameplay loop.
    /// </summary>
    public enum TimeOfDay
    {
        EarlyMorning,   // 5:00 - 7:00  — Chuẩn bị
        Morning,        // 7:00 - 11:00 — Mua nguyên liệu
        Afternoon,      // 11:00 - 17:00 — Nghỉ ngơi, sửa xe
        Evening,        // 17:00 - 20:00 — Chuẩn bị bán
        Night,          // 20:00 - 1:00  — Bán trà đá
        LateNight       // 1:00 - 5:00   — Dọn dẹp, nghỉ
    }

    /// <summary>
    /// Loại thời tiết — ảnh hưởng gameplay và bầu không khí.
    /// </summary>
    public enum WeatherType
    {
        Clear,          // Đêm bình thường
        LightRain,      // Mưa nhẹ
        HeavyRain,      // Mưa lớn
        SeaWind,        // Gió biển
        Foggy           // Sương mù biển
    }

    /// <summary>
    /// Trạng thái của nhân vật chính.
    /// </summary>
    public enum PlayerState
    {
        Idle,           // Đứng yên
        Walking,        // Đi bộ
        Running,        // Chạy
        PushingCart,     // Đẩy xe trà
        Serving,        // Phục vụ khách
        Sitting,        // Ngồi nghỉ
        Interacting     // Đang tương tác
    }

    /// <summary>
    /// Loại NPC khách hàng.
    /// </summary>
    public enum NPCType
    {
        Fisherman,      // Ngư dân
        BusDriver,      // Tài xế xe buýt
        Worker,         // Công nhân
        IslandTraveler, // Khách đi đảo
        LocalResident   // Dân địa phương
    }

    /// <summary>
    /// Trạng thái hành vi NPC.
    /// </summary>
    public enum NPCState
    {
        Spawning,       // Đang xuất hiện
        WalkingIn,      // Đang đi tới
        Approaching,    // Tiến lại xe trà
        SittingDown,    // Đang ngồi xuống
        Ordering,       // Đang gọi món
        Waiting,        // Đang chờ
        Drinking,       // Đang uống
        Paying,         // Đang trả tiền
        LeavingHappy,   // Rời đi vui vẻ
        LeavingSad,     // Rời đi không vui (chờ quá lâu)
        WalkingOut      // Đang đi ra
    }

    /// <summary>
    /// Loại tương tác.
    /// </summary>
    public enum InteractionType
    {
        ServeCustomer,  // Phục vụ khách
        BuySupplies,    // Mua nguyên liệu
        RepairCart,     // Sửa xe
        SitDown,        // Ngồi nghỉ
        Talk,           // Nói chuyện
        PickUp,         // Nhặt đồ
        UseItem         // Sử dụng vật phẩm
    }

    /// <summary>
    /// Cảm xúc trong đối thoại.
    /// </summary>
    public enum DialogueEmotion
    {
        Neutral,        // Bình thường
        Happy,          // Vui
        Sad,            // Buồn
        Worried,        // Lo lắng
        Tired,          // Mệt mỏi
        Hopeful,        // Hy vọng
        Angry,          // Giận
        Grateful        // Biết ơn
    }

    /// <summary>
    /// Mức độ "thất bại cảm xúc" — ảnh hưởng bầu không khí.
    /// </summary>
    public enum EmotionalLevel
    {
        Hopeful,        // Đầy hy vọng — nhiều khách, đèn sáng
        Normal,         // Bình thường
        Struggling,     // Khó khăn — ít khách hơn
        Lonely,         // Cô đơn — rất ít khách, đèn mờ
        Desperate       // Tuyệt vọng — gần như không có khách
    }
}
