using UnityEngine;

namespace GanhHangRong.Core
{
    public static class ChapterOrderCatalog
    {
        public const int TraDa = 0;
        public const int CoffeeDa = 1;
        public const int BanhMiMuoiOt = 20;
        public const int BanhTrangNuong = 21;
        public const int HaiSanXien = 22;
        public const int BunCaKienGiang = 30;
        public const int BanhCanhGhe = 31;
        public const int TomRimNuocMam = 32;
        public const int MucNuongMuoiOt = 33;
        public const int NgheuXaoCay = 34;
        public const int NuocMia = 40;
        public const int TraChanh = 41;
        public const int NuocDua = 42;

        private readonly struct OrderInfo
        {
            public readonly int Id;
            public readonly string Name;
            public readonly int Price;
            public readonly string Recipe;
            public readonly string PrepareFeedback;

            public OrderInfo(int id, string name, int price, string recipe, string prepareFeedback)
            {
                Id = id;
                Name = name;
                Price = price;
                Recipe = recipe;
                PrepareFeedback = prepareFeedback;
            }
        }

        private static readonly OrderInfo[] Chapter1Orders =
        {
            new OrderInfo(
                TraDa,
                "Trà đá",
                Constants.TRA_DA_SELL_PRICE,
                "Công thức: 1 ly sạch, 50g trà, 200ml nước sôi, đá.\n\n1. Lấy ly sạch.\n2. Nhấp bình trà để cho 50g trà.\n3. Đun/rót 200ml nước sôi từ ấm.\n4. Thêm đá từ thùng đá.\n5. Nhấn Space để phục vụ.",
                "-1 ly trà đá"),
            new OrderInfo(
                CoffeeDa,
                "Cà phê đá",
                Constants.COFFEE_SELL_PRICE,
                "Công thức: 1 ly sạch, 30g cà phê, 200ml nước sôi, đá.\n\n1. Lấy ly sạch.\n2. Nhấp hũ cà phê để cho 30g cà phê.\n3. Đun/rót 200ml nước sôi từ ấm.\n4. Thêm đá từ thùng đá.\n5. Nhấn Space để phục vụ.",
                "-1 ly cà phê đá")
        };

        private static readonly OrderInfo[] Chapter2Orders =
        {
            new OrderInfo(
                BanhMiMuoiOt,
                "Bánh mì nướng muối ớt",
                18000,
                "Công thức: bánh mì, sa tế muối ớt, mỡ hành, chà bông.\n\n1. Kẹp bánh mì lên vỉ nướng.\n2. Phết sa tế muối ớt và mỡ hành.\n3. Nướng vàng hai mặt.\n4. Rắc chà bông, cắt miếng.\n5. Nhấn Space để phục vụ.",
                "-1 phần bánh mì"),
            new OrderInfo(
                BanhTrangNuong,
                "Bánh tráng nướng",
                20000,
                "Công thức: bánh tráng, trứng, hành, ruốc, tương ớt.\n\n1. Đặt bánh tráng lên bếp than.\n2. Đập trứng và tán đều mặt bánh.\n3. Thêm hành, ruốc và topping.\n4. Nướng giòn mép bánh.\n5. Nhấn Space để phục vụ.",
                "-1 phần bánh tráng"),
            new OrderInfo(
                HaiSanXien,
                "Hải sản xiên que",
                25000,
                "Công thức: xiên hải sản, muối ớt xanh, than nóng.\n\n1. Chọn xiên hải sản tươi.\n2. Quét sốt muối ớt xanh.\n3. Nướng đều trên than.\n4. Trở xiên đến khi chín thơm.\n5. Nhấn Space để phục vụ.",
                "-1 xiên hải sản")
        };

        private static readonly OrderInfo[] MarketFoodOrders =
        {
            new OrderInfo(
                BunCaKienGiang,
                "Bún Cá Kiên Giang",
                42000,
                "Nguyên liệu: cá lóc, bún tươi, nước mắm Phú Quốc và ớt bột.\n\nMua sách công thức tại quầy Đặc Sản Kiên Giang để mở khóa, sau đó mua đủ nguyên liệu và nấu trong Sổ Công Thức.",
                "-1 tô bún cá Kiên Giang"),
            new OrderInfo(
                BanhCanhGhe,
                "Bánh Canh Ghẹ",
                52000,
                "Nguyên liệu: ghẹ xanh, sợi bánh canh, nước mắm Phú Quốc và muối.\n\nMua sách công thức tại quầy Đặc Sản Kiên Giang để mở khóa, sau đó mua đủ nguyên liệu và nấu trong Sổ Công Thức.",
                "-1 tô bánh canh ghẹ"),
            new OrderInfo(
                TomRimNuocMam,
                "Tôm Rim Nước Mắm",
                65000,
                "Nguyên liệu: tôm, nước mắm Phú Quốc, đường thốt nốt và dầu ăn.",
                "-1 phần tôm rim nước mắm"),
            new OrderInfo(
                MucNuongMuoiOt,
                "Mực Nướng Muối Ớt",
                48000,
                "Nguyên liệu: mực, muối, ớt bột và dầu ăn.",
                "-1 phần mực nướng muối ớt"),
            new OrderInfo(
                NgheuXaoCay,
                "Nghêu Xào Cay",
                52000,
                "Nguyên liệu: nghêu, nước mắm Phú Quốc, ớt bột và dầu ăn.",
                "-1 phần nghêu xào cay"),
            new OrderInfo(
                NuocMia,
                "Nước Mía",
                15000,
                "Nguyên liệu: 2 phần mía cây. Pha trong Sổ Công Thức rồi phục vụ bằng Space.",
                "-1 ly nước mía"),
            new OrderInfo(
                TraChanh,
                "Trà Chanh",
                28000,
                "Nguyên liệu: lá trà, chanh tươi và đường thốt nốt. Pha trong Sổ Công Thức rồi phục vụ bằng Space.",
                "-1 ly trà chanh"),
            new OrderInfo(
                NuocDua,
                "Nước Dừa",
                18000,
                "Nguyên liệu: 1 trái dừa tươi. Chuẩn bị trong Sổ Công Thức rồi phục vụ bằng Space.",
                "-1 trái dừa tươi")
        };

        private static readonly OrderInfo[] DailyOrders =
        {
            Chapter1Orders[0],
            Chapter1Orders[1],
            MarketFoodOrders[0],
            MarketFoodOrders[1],
            MarketFoodOrders[2],
            MarketFoodOrders[3],
            MarketFoodOrders[4],
            MarketFoodOrders[5],
            MarketFoodOrders[6],
            MarketFoodOrders[7]
        };

        public static int GetRandomOrderId(int chapter)
        {
            return GetRandomOrderFrom(DailyOrders);
        }

        public static int GetRandomDailyDrinkId()
        {
            return GetRandomOrderFrom(DailyOrders);
        }

        private static int GetRandomOrderFrom(OrderInfo[] orders)
        {
            var activeIds = UI.TabMenuUI.GetActiveServingOrderIds();
            if (activeIds != null && activeIds.Count > 0)
            {
                System.Collections.Generic.List<OrderInfo> validOrders = new System.Collections.Generic.List<OrderInfo>();
                foreach (var order in orders)
                {
                    if (activeIds.Contains(order.Id))
                    {
                        validOrders.Add(order);
                    }
                }
                if (validOrders.Count > 0)
                {
                    return validOrders[Random.Range(0, validOrders.Count)].Id;
                }
            }

            return orders[Random.Range(0, orders.Length)].Id;
        }

        public static string GetOrderName(int orderId)
        {
            return TryFindOrder(orderId, out OrderInfo info) ? info.Name : "Món ăn";
        }

        public static int GetOrderPrice(int orderId)
        {
            return TryFindOrder(orderId, out OrderInfo info) ? info.Price : Constants.TRA_DA_SELL_PRICE;
        }

        public static string GetOrderRecipe(int orderId)
        {
            return TryFindOrder(orderId, out OrderInfo info) ? info.Recipe : Chapter1Orders[0].Recipe;
        }

        public static string GetPrepareFeedback(int orderId)
        {
            return TryFindOrder(orderId, out OrderInfo info) ? info.PrepareFeedback : "-1 phần";
        }

        public static bool IsChapter2Order(int orderId)
        {
            return orderId == BanhMiMuoiOt ||
                   orderId == BanhTrangNuong ||
                   orderId == HaiSanXien ||
                   orderId == BunCaKienGiang ||
                   orderId == BanhCanhGhe ||
                   orderId == TomRimNuocMam ||
                   orderId == MucNuongMuoiOt ||
                   orderId == NgheuXaoCay ||
                   IsMarketDrink(orderId);
        }

        public static bool IsFoodOrder(int orderId)
        {
            return orderId == BanhMiMuoiOt ||
                   orderId == BanhTrangNuong ||
                   orderId == HaiSanXien ||
                   orderId == BunCaKienGiang ||
                   orderId == BanhCanhGhe ||
                   orderId == TomRimNuocMam ||
                   orderId == MucNuongMuoiOt ||
                   orderId == NgheuXaoCay;
        }

        public static bool IsMarketDrink(int orderId)
        {
            return orderId == NuocMia || orderId == TraChanh || orderId == NuocDua;
        }

        private static bool TryFindOrder(int orderId, out OrderInfo info)
        {
            for (int i = 0; i < Chapter1Orders.Length; i++)
            {
                if (Chapter1Orders[i].Id == orderId)
                {
                    info = Chapter1Orders[i];
                    return true;
                }
            }

            for (int i = 0; i < Chapter2Orders.Length; i++)
            {
                if (Chapter2Orders[i].Id == orderId)
                {
                    info = Chapter2Orders[i];
                    return true;
                }
            }

            for (int i = 0; i < MarketFoodOrders.Length; i++)
            {
                if (MarketFoodOrders[i].Id == orderId)
                {
                    info = MarketFoodOrders[i];
                    return true;
                }
            }

            info = default;
            return false;
        }
    }
}
