using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IBookingService
    {
        Task<List<TimeSlotResponseDTO>> GetAvailableSlotsAsync(int userId, DateTime targetDate);
        Task<BookingResponseDTO> GetBookingByIdAsync(int userId, int bookingId);
        Task<BookingResponseDTO> CreateBookingAsync(int userId, CreateBookingDTO request);
        Task<List<BookingResponseDTO>> GetAllBookingsByDateAsync(DateTime targetDate);
        Task<bool> UpdateBookingStatusAsync(int bookingId, string newStatus);
        Task<List<BookingResponseDTO>> GetMyBookingsAsync(int userId);
        Task<bool> CancelBookingAsync(int userId, int bookingId);
    }
}