# SmartWash API Integration Guide

This document is designed for frontend developers to understand how to integrate with the SmartWash API. It is structured by **User Flows**, providing a step-by-step guide on which APIs to call, in what order, and how they work together to achieve specific business features.

> **Base URL:** `http(s)://<your-server-domain>`
> **Global Response Format:**
> Almost all APIs return a standard JSON structure:
> ```json
> {
>   "statusCode": 200,
>   "message": "Success",
>   "data": { ... },
>   "details": null
> }
> ```
> *Note: If `statusCode` is 400 or higher, check `message` and `details` for error explanations.*

> **Authentication Header:**
> For any endpoint marked as requiring authentication, include the JWT token in the request headers:
> `Authorization: Bearer <your_access_token>`

---

## 1. Authentication & User Management Flow

This flow handles user registration, login, and profile retrieval.

### Step 1: Register a new Customer Account
**Endpoint:** `POST /api/v1/auth/register`
**Auth Required:** No

Users must provide their details to create an account. The system validates passwords (minimum 8 characters, 1 uppercase, 1 number) and phone numbers.
*   **Request Payload Example:**
    ```json
    {
      "phoneNumber": "0912345678",
      "email": "user@example.com",
      "password": "Password123",
      "fullName": "Nguyen Van A"
    }
    ```
*   **Expected Response:** `201 Created` with a success message.

### Step 2: Login to obtain Tokens
**Endpoint:** `POST /api/v1/auth/login`
**Auth Required:** No

Users can log in using either their `Phone` or `Email`.
*   **Request Payload Example:**
    ```json
    {
      "phoneOrEmail": "0912345678",
      "password": "Password123"
    }
    ```
*   **Expected Response:** Returns an `AuthResponseDTO` which includes the `Token` (JWT Access Token) and `RefreshToken`. The Access Token should be stored (e.g., localStorage) and used in the `Authorization: Bearer <token>` header for all subsequent protected requests.

### Step 3: Fetch the User's Profile
**Endpoint:** `GET /api/v1/users/me`
**Auth Required:** Yes

Once logged in, fetch the user's details, including their current Loyalty Tier, Points, Wallet Balance (if any), and registered Vehicles.
*   **Response Data Example:**
    ```json
    {
      "userId": 1,
      "fullName": "Nguyen Van A",
      "phoneNumber": "0912345678",
      "tierName": "Standard",
      "totalPoint": 150,
      "promotionPoint": 50,
      "vehicles": [
         { "licensePlate": "51H12345", "vehicleType": "Sedan" }
      ]
    }
    ```

### Step 4: Refresh Token (When Access Token Expires)
**Endpoint:** `POST /api/v1/auth/refresh-token`
**Auth Required:** No

Access tokens expire quickly. When a request returns `401 Unauthorized`, use this endpoint with your saved `RefreshToken` to get a new pair of tokens.

---

## 2. Vehicle Management Flow

Customers need to register their vehicles before they can create a booking. A customer can add unlimited vehicles to their profile.

### Step 1: Add a Vehicle
**Endpoint:** `POST /api/v1/vehicles`
**Auth Required:** Yes

The customer registers a new vehicle. The `LicensePlate` is standardized automatically on the backend (spaces/dashes removed, uppercase).
*   **Request Payload Example:**
    ```json
    {
      "licensePlate": "51H-123.45",
      "vehicleTypeId": 1
    }
    ```
*(Note: `vehicleTypeId` can be retrieved via `GET /api/v1/admin/vehicle-types` if public, or pre-fetched by the frontend).*

### Step 2: View My Vehicles
**Endpoint:** `GET /api/v1/vehicles`
**Auth Required:** Yes

Retrieves a list of all vehicles registered to the currently authenticated user.
*   **Response Data Example:**
    ```json
    [
      {
        "licensePlate": "51H12345",
        "vehicleType": "Sedan"
      }
    ]
    ```

### Step 3: Update or Delete Vehicle (Optional)
**Update:** `PUT /api/v1/vehicles/{licensePlate}`
**Delete:** `DELETE /api/v1/vehicles/{licensePlate}`
**Auth Required:** Yes

Used if the user wants to change the `vehicleTypeId` or remove a car from their account.

---

## 3. Customer Booking Process Flow

This flow covers how a frontend app builds a booking (selecting services, checking dates, applying points/vouchers, and confirming). The system uses a "shopping cart" style architecture where a single booking can include multiple vehicles.

### Step 1: Fetch Available Services
**Endpoint:** `GET /api/v1/services`
**Auth Required:** No

Retrieve a list of active services that the user can choose from.
*   **Response Data Example:**
    ```json
    [
      {
        "serviceId": 1,
        "serviceName": "Standard Wash",
        "description": "Exterior wash and dry",
        "prices": [
          { "vehicleTypeId": 1, "vehicleTypeName": "Sedan", "price": 100000, "capacityWeight": 1.0 }
        ]
      }
    ]
    ```

### Step 2: Check Available Time Slots for a Date
**Endpoint:** `GET /api/v1/bookings/slots?targetDate=2023-12-01`
**Auth Required:** Yes

Fetches all time slots for a specific date and indicates if they are `IsAvailable` based on capacity limits and the customer's current Loyalty Tier (some slots might be VIP only).
*   **Response Data Example:**
    ```json
    [
      {
        "slotId": 1,
        "timeRange": "08:00 - 09:00",
        "isAvailable": true,
        "reason": null
      },
      {
        "slotId": 2,
        "timeRange": "09:00 - 10:00",
        "isAvailable": false,
        "reason": "Kín chỗ"
      }
    ]
    ```

### Step 3: Create the Booking
**Endpoint:** `POST /api/v1/bookings`
**Auth Required:** Yes

Submit the booking. Ensure the `targetDate` is converted to UTC (`.ToUniversalTime()`) if applicable, though the backend compares strictly by Date in some contexts. The backend checks for sufficient Wallet balance to hold the deposit. If the user doesn't have enough money in their Wallet, this will fail (see Wallet Flow).
*   **Request Payload Example:**
    ```json
    {
      "scheduledDate": "2023-12-01T00:00:00Z",
      "slotId": 1,
      "pointsToUse": 0,
      "voucherId": null,
      "vehicles": [
        { "licensePlate": "51H12345", "serviceId": 1 }
      ]
    }
    ```
*   **Expected Response:** `200 OK` indicating the booking is created and the cost has been deducted from the user's wallet.

### Step 4: View My Bookings / History
**Endpoint:** `GET /api/v1/bookings/me`
**Auth Required:** Yes

Retrieves all bookings made by the customer. Includes statuses like `Pending`, `CheckedIn`, `Completed`, `Cancelled`, `NoShow`.

### Step 5: Cancel Booking (Optional)
**Endpoint:** `PUT /api/v1/bookings/{id}/cancel`
**Auth Required:** Yes

Customers can cancel pending bookings. If canceled early enough, their wallet deposit is refunded.

---

## 4. Wallet & Payment Flow

The system uses an internal Wallet model. Customers must top up their wallet via PayOS before making bookings.

### Step 1: Check Wallet Balance
**Endpoint:** `GET /api/v1/wallets/me`
**Auth Required:** Yes

Fetches the user's current fiat `Balance` and point balances.
*   **Response Data Example:**
    ```json
    {
      "balance": 150000.0,
      "totalPoints": 100,
      "promotionPoints": 50
    }
    ```

### Step 2: Request a Top Up
**Endpoint:** `POST /api/v1/wallets/top-up`
**Auth Required:** Yes

Initiates a payment session with PayOS. The frontend provides redirect URLs.
*   **Request Payload Example:**
    ```json
    {
      "amount": 200000.0,
      "cancelUrl": "https://yourfrontend.com/payment/cancel",
      "returnUrl": "https://yourfrontend.com/payment/success"
    }
    ```
*   **Expected Response:** Returns a `checkoutUrl`. The frontend should redirect the user's browser to this URL to complete the payment via PayOS.

### Step 3: Handle Payment Callback (Webhook)
**Endpoint:** `POST /api/v1/wallets/top-up/callback`
**Auth Required:** No (Handled by PayOS server)

*Frontend Developers do not call this endpoint.* PayOS calls it automatically upon successful payment to update the user's wallet `Balance`. When the user returns to `returnUrl`, the frontend should poll or refetch `GET /api/v1/wallets/me` to see the updated balance.

### Step 4: View Transaction History
**Endpoint:** `GET /api/v1/transactions`
**Auth Required:** Yes

Shows a history of top-ups, booking deductions, refunds, and upsell charges.

---

## 5. Loyalty & Promotions Flow

Handles VIP Tiers, Points accumulation, and Vouchers.

### Step 1: View Loyalty Tiers
**Endpoint:** `GET /api/v1/tiers`
**Auth Required:** No

Returns the list of available VIP Tiers and the `MinAccumulatedPoints` required to reach them. This can be used on a "Benefits" page.
*   **Response Data Example:**
    ```json
    [
      { "tierId": 1, "tierName": "Standard", "pointMultiplier": 1.0, "minAccumulatedPoints": 0 },
      { "tierId": 2, "tierName": "Gold", "pointMultiplier": 1.5, "minAccumulatedPoints": 1000 }
    ]
    ```

### Step 2: View Point History
**Endpoint:** `GET /api/v1/points/history`
**Auth Required:** Yes

Shows the ledger of points awarded (from completed bookings) and points deducted (from using points to discount bookings or from expirations).

### Step 3: View Available Vouchers
**Endpoint:** `GET /api/v1/vouchers/me`
**Auth Required:** Yes

Shows a list of vouchers the customer can apply to a booking.

### Step 4: Redeem / Exchange a Voucher
**Endpoint:** `POST /api/v1/vouchers/redeem`
**Auth Required:** Yes

Allows a user to exchange their `PromotionPoints` for a Voucher.
*   **Request Payload Example:**
    ```json
    {
      "voucherCode": "SUMMER10"
    }
    ```

---

## 6. Staff Operations Flow

This flow is for internal staff managing vehicles at the physical car wash location. It covers the lifecycle of a car from arrival to completion.

### Step 1: View Daily Schedule
**Endpoint:** `GET /api/v1/admin/bookings?targetDate=2023-12-01`
**Auth Required:** Yes (Role: Staff or Admin)

Fetches all bookings scheduled for a specific date so staff can see who is coming.

### Step 2: Check-In Customer
**Endpoint:** `PUT /api/v1/admin/bookings/{id}/status?newStatus=CheckedIn`
**Auth Required:** Yes (Role: Staff or Admin)

When the customer arrives, the staff updates the booking status from `Pending` to `CheckedIn`.

### Step 3: Handle Walk-in Bookings (Optional)
**Endpoint:** `POST /api/v1/bookings/walk-in`
**Auth Required:** Yes (Role: Staff or Admin)

If a customer arrives without a booking, staff can use this endpoint to bypass time-slot capacity rules and force a booking into the system.

### Step 4: Update Vehicle Condition / Upsell (Optional)
**Endpoint:** `PUT /api/v1/bookings/{bookingId}/condition`  *(Called from BookingsController)*
**Auth Required:** Yes (Role: Staff, Manager, Admin)

If the vehicle is exceptionally dirty (e.g., Muddy), the staff can update the condition. The system will automatically calculate an upsell surcharge and attempt to deduct it from the customer's wallet.
*   **Request Payload Example:**
    ```json
    {
      "detailId": 12,
      "condition": "Muddy"
    }
    ```

### Step 5: Complete Booking
**Endpoint:** `PUT /api/v1/admin/bookings/{id}/status?newStatus=Completed`
**Auth Required:** Yes (Role: Staff or Admin)

Once the wash is done, the staff marks it as `Completed`. This triggers the backend to officially close the transaction and award Loyalty points to the customer.

### Step 6: Mark as No-Show
**Endpoint:** `PUT /api/v1/admin/bookings/{id}/no-show`
**Auth Required:** Yes (Role: Staff or Admin)

If a customer never arrives, the staff marks the booking as a No-Show. The system retains the deposit and may apply a penalty to the customer's Churn Score.

---

## 7. Admin Operations Flow

These endpoints are strictly for administrators configuring the system parameters.

**Global Auth Requirement:** Yes (Role: Admin)

### Managing Vehicle Types
Allows the admin to define what types of vehicles the system supports (e.g., Sedan, SUV, Motorcycle).
*   **Create:** `POST /api/v1/admin/vehicle-types`
*   **Update:** `PUT /api/v1/admin/vehicle-types/{id}`
*   **List:** `GET /api/v1/admin/vehicle-types`

### Managing Services
Defines the wash services, their descriptions, and sets specific prices based on `VehicleType`.
*   **Create:** `POST /api/v1/admin/services`
*   **Update:** `PUT /api/v1/admin/services/{id}`
*   **Toggle Active/Inactive:** `DELETE /api/v1/admin/services/{id}`

### Managing Vouchers
Allows the admin to issue new discount vouchers that users can redeem with points.
*   **Create:** `POST /api/v1/admin/vouchers`
*   **Update:** `PUT /api/v1/admin/vouchers/{id}`
*   **List All:** `GET /api/v1/admin/vouchers`

### User Management
Used by admins to view customer details, ban accounts, or review history.
*   **List Customers (Paginated):** `GET /api/v1/admin/users?page=1&pageSize=10`
*   **View Specific Customer Detail:** `GET /api/v1/admin/users/{id}`
*   **Update User Status (e.g., Ban):** `PUT /api/v1/admin/users/{id}/status`

---

## Frontend Integration Guide: The Business Flows

As the Backend Tech Lead, I have structured this guide to explain the **Business Flows** rather than just listing endpoints. Read this carefully to understand how data flows through the SmartWash system.

### 1. User Registration Flow
**Endpoint:** `POST /api/v1/auth/register`
**1. Business Purpose:** Creates a new Customer account along with an empty Wallet and Profile.
**2. Prerequisites (Crucial):** None. This is the entry point.
**3. Request Payload:**
```json
{
  "phoneNumber": "0912345678",
  "email": "user@example.com",
  "password": "Password123",
  "fullName": "Nguyen Van A"
}
```
**4. Expected Response & Error Handling:**
*   **Success (201):** User is created.
*   **Error (400):** Watch out for "Email/Phone already exists" or "Password does not meet complexity requirements" (requires 8 chars, 1 uppercase, 1 number).
**5. Next Steps:** Automatically redirect the user to the Login screen.
**6. ⚠️ Critical Warnings for FE:** Do not log the user in automatically after registration. Force them through the explicit login flow to establish JWT tokens.

### 2. Authentication & Token Management
**Endpoint:** `POST /api/v1/auth/login`
**1. Business Purpose:** Authenticates the user and provisions JWT Access & Refresh tokens.
**2. Prerequisites (Crucial):** User must exist.
**3. Request Payload:**
```json
{
  "phoneOrEmail": "0912345678",
  "password": "Password123"
}
```
**4. Expected Response & Error Handling:**
*   **Success (200):** Returns `Token` (15m expiry) and `RefreshToken` (7d expiry).
*   **Error (401):** "Invalid credentials".
*   **Error (429):** "Too many requests" (Brute-force protection is active, tell user to wait).
**5. Next Steps:** Store tokens securely (e.g., localStorage/secure cookies). Call `GET /api/v1/users/me` and `GET /api/v1/wallets/me` to hydrate the app state.
**6. ⚠️ Critical Warnings for FE:** Access tokens expire quickly. You MUST implement an axios interceptor (or similar) to catch `401 Unauthorized` errors, call `POST /api/v1/auth/refresh-token`, and automatically retry the failed request without logging the user out.

### 3. Vehicle Registration Flow
**Endpoint:** `POST /api/v1/vehicles`
**1. Business Purpose:** Registers a customer's car to their profile so it can be selected during booking.
**2. Prerequisites (Crucial):** You must call `GET /api/v1/admin/vehicle-types` first to populate a dropdown so the user can select their `vehicleTypeId` (e.g., Sedan, SUV).
**3. Request Payload:**
```json
{
  "licensePlate": "51H-123.45",
  "vehicleTypeId": 1
}
```
**4. Expected Response & Error Handling:**
*   **Success (200):** Vehicle added. Backend automatically normalizes the plate (e.g., "51H12345").
*   **Error (400):** "License plate already registered."
**5. Next Steps:** Call `GET /api/v1/vehicles` to refresh the user's garage UI.
**6. ⚠️ Critical Warnings for FE:** The backend relies on soft-deletes. If a user deleted a car previously and tries to re-add it, the backend handles the recovery, but FE just treats it as a standard success.

### 4. Wallet Top-Up Flow (PayOS Integration)
**Endpoint:** `POST /api/v1/wallets/top-up`
**1. Business Purpose:** Generates a payment session to add fiat currency to the user's SmartWash wallet.
**2. Prerequisites (Crucial):** FE must know the environment's callback URLs.
**3. Request Payload:**
```json
{
  "amount": 200000.0,
  "cancelUrl": "https://yourapp.com/payment/cancel",
  "returnUrl": "https://yourapp.com/payment/success"
}
```
**4. Expected Response & Error Handling:**
*   **Success (200):** Returns a `checkoutUrl`.
*   **Error (400):** Invalid amount (must be positive).
**5. Next Steps:** Redirect the browser/webview entirely to the `checkoutUrl`.
**6. ⚠️ Critical Warnings for FE:** **ASYNCHRONOUS COMPLETION!** When the user lands back on your `returnUrl`, the money might NOT be in their wallet yet. The backend relies on PayOS firing a webhook. FE MUST implement a polling mechanism (calling `GET /api/v1/wallets/me` every 3 seconds) until the `balance` increases, before showing "Success" to the user.

### 5. Master Booking Flow (The Cart Architecture)
**Endpoint:** `POST /api/v1/bookings`
**1. Business Purpose:** Deducts funds from the wallet and locks in a time slot for washing one or multiple vehicles.
**2. Prerequisites (Crucial):** This is the most complex API. FE must gather:
    *   `GET /api/v1/vehicles` -> to get `LicensePlate`.
    *   `GET /api/v1/services` -> to let user pick `serviceId`.
    *   `GET /api/v1/bookings/slots?targetDate=...` -> to pick an available `slotId`.
    *   `GET /api/v1/wallets/me` -> to ensure Balance >= Total Price.
**3. Request Payload:**
```json
{
  "scheduledDate": "2023-12-01T00:00:00Z",
  "slotId": 1,
  "pointsToUse": 0,
  "voucherId": null,
  "vehicles": [
    { "licensePlate": "51H12345", "serviceId": 1 }
  ]
}
```
**4. Expected Response & Error Handling:**
*   **Success (200):** Booking confirmed, wallet deducted.
*   **Error (400):** "Insufficient balance" (redirect user to Top-up).
*   **Error (400):** "Vehicle already has pending booking" (Anti-hoarding rule).
*   **Error (400):** "Slot capacity exceeded" (someone booked it fractions of a second before them).
**5. Next Steps:** Redirect user to the "Booking History" screen and call `GET /api/v1/bookings/me`.
**6. ⚠️ Critical Warnings for FE:** Ensure `scheduledDate` is sent in UTC formatting but represents the correct local day. Validate the user's wallet balance locally *before* enabling the "Submit Booking" button to prevent unnecessary API failures.

### 6. Tier & Loyalty Points Processing
**Endpoint:** Background / Implicit via `GET /api/v1/users/me`
**1. Business Purpose:** Rewards users for loyalty and evaluates VIP upgrades.
**2. Prerequisites (Crucial):** Points are only awarded AFTER a booking status is updated to `Completed` by the Staff.
**3. Request Payload:** N/A (Read-only for Customer FE).
**4. Expected Response & Error Handling:** `TotalPoint` (lifetime) and `PromotionPoint` (spendable) will increase.
**5. Next Steps:** Update UI badges (e.g., "Gold Member"). Users can spend points in two ways:
    *   **Direct Discount:** Pass `pointsToUse` > 0 in the `POST /api/v1/bookings` payload.
    *   **Redeem Voucher:** Call `POST /api/v1/vouchers/redeem` to convert points into a discount code.
**6. ⚠️ Critical Warnings for FE:** Tiers (`CurrentYearTierPoints`) reset automatically every January 1st via a backend worker. FE should read the tier requirements and warn users in December: *"Your points expire soon, book now!"* Furthermore, points expire via FIFO logic. Do not display points as "pending" during a wash; they do not exist until the wash is finalized.

---
*End of Document*
