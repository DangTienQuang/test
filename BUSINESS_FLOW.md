# Business (B2B) System Flow Documentation

This document outlines the end-to-end flow for B2B Company/Business users in the Smart Automated Car Wash Management System. It covers everything from initial registration to vehicle management, booking, operations, and billing.

## 1. Business Registration & Approval Flow

1. **Registration (Business)**
   - The business representative submits a registration request via the `POST /api/v1/business/register` endpoint.
   - The payload includes company details (Company Name, Tax Code, Address, Billing Email, etc.), representative info, and uploaded documents (Business License, Authorization Letter).
   - The system creates a `User` account with a `Business` role and a `BusinessProfile` entity with an initial `ApprovalStatus` of `"Pending"`.

2. **Admin Review (Admin)**
   - Administrators view pending applications via `GET /api/v1/business/admin/pending-applications`.
   - Admin reviews the application (`POST /api/v1/business/admin/review-application`) and can either:
     - **Approve**: The `ApprovalStatus` becomes `"Approved"`. Contract details (Credit Limit, Discount Percent, Start/End dates, Payment Term) are set, and the `IsContractActive` flag becomes `true`.
     - **Reject**: The `ApprovalStatus` becomes `"Rejected"` with a provided `RejectionReason`.

## 2. Fleet Import & Vehicle Management Flow

1. **Fleet Import (Business)**
   - Approved businesses can bulk upload their fleet using an Excel template via `POST /api/v1/fleet/import`.
   - The system parses the file, creates a `FleetImportBatch`, and inserts each valid vehicle as a `FleetVehicle` with a status of `"Pending"`.
   - Invalid rows generate `FleetImportError` records for the batch.

2. **Vehicle Approval (Admin)**
   - Admins fetch pending fleet vehicles across the system.
   - For each vehicle, the admin can approve (`ApproveFleetVehicleAsync`) or reject (`RejectFleetVehicleAsync`).
   - Only approved `FleetVehicle` records can be booked for services.

## 3. Business Booking Flow

1. **Check Availability**
   - The business checks available time slots (`GET /api/v1/business/available-slots`) for their fleet.

2. **Create Booking**
   - The business creates a booking (`POST /api/v1/business/bookings`), specifying the vehicles to be washed and the desired services.
   - A multi-vehicle booking is processed, generating necessary `Booking` records linked to the business.

3. **Rescheduling / Cancellation**
   - Bookings can be rescheduled (`PUT /api/v1/business/reschedule/{id}`) to adjust the `ScheduledTime` and associated `DailySlotCapacity` without recalculating pricing or wallet/payment transactions.
   - Bookings can also be cancelled before the scheduled time.

## 4. Wash Operations Flow (On-Site)

1. **Check-In / Walk-In**
   - **Pre-booked Check-In**: When the fleet vehicle arrives, it is checked in, transitioning the `Booking` status to `"CheckedIn"` and creating/updating a `FleetWashLog`.
   - **Walk-In**: If a fleet vehicle arrives without a prior booking, a `Walk-In` process creates the booking and check-in simultaneously.

2. **Lane Assignment & Processing**
   - Staff/Managers assign the vehicle to a specific lane (`POST /api/v1/business/washlogs/{washLogId}/assign-lane`).
   - The wash starts (`StartProcessingAsync`), moving the log/booking status to `"Processing"`.

3. **Check-Out / Walk-Out**
   - Upon completion of the wash, the vehicle is checked out (`CheckOutAsync`), marking the `Booking` as `"Completed"` and finishing the `FleetWashLog`.
   - The cost of the services is calculated and recorded against the business's current month usage (`CurrentMonthUsage`).

## 5. Billing & Invoicing Flow

1. **Monthly Statements & Invoice Generation**
   - The system tracks all completed washes within a billing cycle.
   - At the end of the month (or via Admin trigger), a monthly invoice is generated (`GenerateMonthlyInvoiceAsync`), summarizing all washes into an `Invoice` with detailed `InvoiceItem` records.
   - The invoice takes into account the business's `DiscountPercent`.

2. **Dashboard & History (Business)**
   - Businesses can view their fleet's wash history and active vehicles on the floor.
   - They can view their monthly statements (`GET /api/v1/business/statements/monthly`) and export invoices as PDFs.
   - The `BusinessProfile.CurrentMonthUsage` resets or rolls over according to the billing cycle logic, ensuring it stays within the `MonthlyCreditLimit`.
