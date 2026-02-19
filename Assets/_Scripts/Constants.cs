public static class Constants
{
    // --- BẢNG CÀI ĐẶT LƯỚI ---
    public const int WIDTH = 9;
    public const int HEIGHT = 9;
    public const float TILE_SIZE = 1f;

    // --- CÀI ĐẶT HOẠT ẢNH ---
    public const float SWAP_DURATION = 0.2f; // Thời gian trượt kẹo (giúp đồng bộ mọi nơi)

    // --- PHÂN LOẠI KẸO ---
    public enum CandyType
    {
        PURPLE, // Index 0
        BLUE,   // Index 1
        GREEN,  // Index 2
        YELLOW, // Index 3
    }

    public enum BonusType
    {
        NONE,
        ROW_CLEAR,
        COLUMN_CLEAR,
        BOMB,
        RAINBOW
    }

    // --- MÁY TRẠNG THÁI (STATE MACHINE) ---
    public enum GameState
    {
        IDLE,       // Trạng thái rảnh rỗi: Cho phép người chơi vuốt kẹo
        SWAP,       // Trạng thái tráo kẹo: Khóa Input để tránh bug click đúp
        RESOLVING,  // Trạng thái xử lý: Đang nổ, rơi kẹo hoặc bù kẹo mới
    }

    // --- HƯỚNG VUỐT (DÙNG CHO GAME MANAGER) ---
    public enum Direction
    {
        NONE,
        UP,
        DOWN,
        LEFT,
        RIGHT,
    }
}