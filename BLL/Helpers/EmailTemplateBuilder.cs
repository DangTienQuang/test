using System;
using System.Collections.Generic;
using System.Linq;
using AutoWashPro.DAL.Entities;
using AutoWashPro.DAL.Enums;

namespace BLL.Helpers
{
    public static class EmailTemplateBuilder
    {
        public static string BuildBookingConfirmationEmail(Booking booking, List<BookingDetail> details, string customerName)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 10px;'>
                <h2 style='color: #007bff; text-align: center;'>SMARTWASH XÁC NHẬN ĐẶT LỊCH</h2>
                <p>Xin chào <b>{customerName}</b>,</p>
                <p>Cảm ơn bạn đã sử dụng dịch vụ của SmartWash. Dưới đây là thông tin lịch hẹn của bạn:</p>

                <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Mã lịch hẹn:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>#{booking.BookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Mã QR Check-in:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee; font-weight: bold;'>{booking.FallbackQrCode}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Biển số xe:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{booking.LicensePlate}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Thời gian:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee; color: red;'><b>{booking.ScheduledTime:dd/MM/yyyy HH:mm}</b></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Tổng tiền:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{booking.OriginalPrice:N0} đ</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Đã thanh toán (Cọc):</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee; color: green;'><b>{booking.FinalAmount:N0} đ</b></td>
                    </tr>
                </table>

                <p style='margin-top: 20px;'>Vui lòng đến trạm đúng giờ. Hãy đưa mã QR Check-in cho nhân viên nếu có sự cố với camera nhận diện.</p>
                <p>Trân trọng,<br><b>Đội ngũ SmartWash</b></p>
            </div>";
        }

        public static string BuildVoucherCampaignEmail(Voucher voucher, string customerName, DateTime userExpiryDate)
        {
            var title = voucher.CampaignType switch
            {
                VoucherCampaignType.Birthday => "Chúc mừng sinh nhật từ SmartWash",
                VoucherCampaignType.Age => "SmartWash gửi bạn voucher đặc biệt",
                VoucherCampaignType.Winback => "Lâu rồi không gặp, SmartWash nhớ bạn",
                VoucherCampaignType.Vip => "Quà riêng cho thành viên VIP",
                VoucherCampaignType.Milestone => "Cảm ơn bạn đã đồng hành cùng SmartWash",
                _ => "SmartWash gửi bạn voucher mới"
            };

            var reason = voucher.CampaignType switch
            {
                VoucherCampaignType.Birthday => "nhân dịp sinh nhật của bạn",
                VoucherCampaignType.Age => voucher.TargetAge.HasValue ? $"nhân dịp bạn tròn {voucher.TargetAge.Value} tuổi" : "dành riêng cho bạn",
                VoucherCampaignType.Winback => "để chào mừng bạn quay lại sử dụng dịch vụ",
                VoucherCampaignType.Vip => "dành riêng cho hạng thành viên của bạn",
                VoucherCampaignType.Milestone => voucher.MilestoneUsageCount.HasValue ? $"vì bạn đã đạt mốc {voucher.MilestoneUsageCount.Value} lần sử dụng dịch vụ" : "vì bạn đã đồng hành cùng chúng tôi",
                _ => "dành riêng cho bạn"
            };

            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 10px;'>
                <h2 style='color: #007bff; text-align: center;'>{title}</h2>
                <p>Xin chào <b>{customerName}</b>,</p>
                <p>SmartWash gửi tặng bạn một voucher {reason}.</p>
                <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Mã voucher:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee; font-weight: bold; color: #007bff;'>{voucher.Code}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Giá trị giảm:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{voucher.DiscountAmount:N0} đ</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Số lượt dùng mỗi khách:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{voucher.MaxUsagePerUser}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Hạn sử dụng:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{userExpiryDate:dd/MM/yyyy}</td>
                    </tr>
                </table>
                <p style='margin-top: 20px;'>Voucher đã được tự động thêm vào ví voucher của bạn. Hãy đặt lịch và chọn voucher khi thanh toán để sử dụng.</p>
                <p>Trân trọng,<br><b>Đội ngũ SmartWash</b></p>
            </div>";
        }
    }
}
