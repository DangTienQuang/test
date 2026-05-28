using System;
using System.Collections.Generic;
using System.Linq;
using AutoWashPro.DAL.Entities;

namespace AutoWashPro.BLL.Helpers
{
    public static class EmailTemplateBuilder
    {
        public static string BuildBookingConfirmationEmail(Booking booking, List<BookingDetail> details, string customerName)
        {
            string plates = string.Join(", ", details.Select(d => d.LicensePlate));

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
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Số lượng xe:</b></td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{details.Count} ({plates})</td>
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
    }
}
