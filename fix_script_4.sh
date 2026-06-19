sed -i 's/BaseWeight = 1.0,/BaseWeight = 1,/g' BLL/Services/VehicleService.cs
sed -i 's/ActiveBookingId = activeBooking?.Id,/ActiveBookingId = activeBooking?.BookingId,/g' BLL/Services/VehicleService.cs
