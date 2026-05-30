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

## The Booking Data Pipeline & Prerequisites

For a customer to successfully create a booking using `POST /api/v1/bookings`, both the Backend Admin configuration and the Frontend data retrieval must happen in a specific order.

The booking system relies on relational data: a vehicle belongs to a specific `VehicleType`, services have prices that vary by `VehicleType`, and bookings are scheduled into `TimeSlots`.

### 1. Admin System Setup (Prerequisites)
Before any bookings can occur, the Admin must configure the master data:
1.  **Create Vehicle Types:** Admin calls `POST /api/v1/admin/vehicle-types` (e.g., "Sedan", "SUV", "Motorcycle"). This generates `vehicleTypeId`s.
2.  **Create Services & Pricing:** Admin calls `POST /api/v1/admin/services`. When creating a service (e.g., "Standard Wash"), the admin must link prices to the existing `vehicleTypeId`s created in step 1. This generates `serviceId`s.
3.  **Generate Time Slots:** Admin configures available business hours, generating `slotId`s that represent blocks of time (e.g., 08:00 - 09:00).

### 2. Frontend Pre-Booking Flow (Data Retrieval)
To build the `CreateBookingDTO` payload, the Frontend must gather IDs from the configured master data.

1.  **Registering the User's Vehicle (Needs `vehicleTypeId`)**
    *   *Action:* FE calls `GET /api/v1/admin/vehicle-types` (or pre-fetches it).
    *   *User Input:* User selects their car type (e.g., Sedan).
    *   *Action:* FE calls `POST /api/v1/vehicles` with `licensePlate` and the selected `vehicleTypeId`.
2.  **Selecting a Service (Needs `serviceId`)**
    *   *Action:* FE calls `GET /api/v1/services`.
    *   *User Input:* User selects "Standard Wash".
    *   *Result:* FE stores the `serviceId` to pass in the booking payload.
3.  **Selecting a Time Slot (Needs `slotId` and `scheduledDate`)**
    *   *Action:* FE calls `GET /api/v1/bookings/slots?targetDate=YYYY-MM-DD`.
    *   *User Input:* User selects an available slot (e.g., 08:00 - 09:00).
    *   *Result:* FE stores the `slotId` and the chosen `scheduledDate`.
4.  **Wallet Preparation (Needs sufficient balance)**
    *   *Action:* FE calls `GET /api/v1/wallets/me`.
    *   *Result:* FE calculates the total price of the selected services. If the wallet balance is lower than the required deposit, the FE must guide the user to `POST /api/v1/wallets/top-up` before submitting the booking.

### 3. Assembling the Booking Payload
Once the above steps are completed, the Frontend has all the necessary relational IDs and funds to execute the booking.

```json
{
  "scheduledDate": "2023-12-01T00:00:00Z", // From Date Picker
  "slotId": 1,                             // From GET /api/v1/bookings/slots
  "pointsToUse": 0,                        // From user input, validated against GET /api/v1/wallets/me
  "voucherId": null,                       // From GET /api/v1/vouchers/me (if applicable)
  "vehicles": [
    {
      "licensePlate": "51H12345",          // From GET /api/v1/vehicles
      "serviceId": 1                       // From GET /api/v1/services
    }
  ]
}
```

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

## Business Logic Pipelines & Frontend Integration Guide

This section is critical for Frontend Developers. It explains the underlying business rules and the exact integration sequences required to build functional Wallet, Loyalty, and Tier systems.

### 1. Wallet & Payment Pipeline

The SmartWash system operates on a prepaid wallet model. Customers must have sufficient funds in their internal wallet before they can execute a booking.

**Prerequisite:** A user's Wallet is generated automatically during the account Registration process.
* **FE Action:** The Frontend should fetch the user's balance by calling `GET /api/v1/wallets/me` immediately upon successful login or app initialization.

**The Payment Flow:**
To add funds to the wallet, the system relies on the third-party PayOS gateway.
1.  **Initiate Top-Up:** The Frontend calls `POST /api/v1/wallets/top-up` with the desired `amount` and the FE redirect URLs (`returnUrl` / `cancelUrl`).
2.  **Redirect to Gateway:** The backend creates a `Pending` transaction and returns a `checkoutUrl`. The Frontend must redirect the user's browser/webview to this PayOS checkout page.

> **CRITICAL WARNING FOR FE: ASYNCHRONOUS PAYMENT COMPLETION**
> Do **NOT** assume the payment is successful just because the user returns to your `returnUrl`. The Top-Up API does not return immediate success to the frontend. The actual wallet balance is updated via an asynchronous server-to-server Webhook (`POST /api/v1/wallets/top-up/callback`).
> **Implementation Requirement:** When the user lands on the `returnUrl`, the Frontend MUST implement polling on `GET /api/v1/wallets/me` (or listen to a WebSocket/SignalR event if implemented) to verify that the `balance` has increased before showing a "Payment Successful" screen to the user.

### 2. Loyalty (Points) & Promotions Pipeline

The points system encourages user retention through rewards.

**Prerequisite (Understanding Points):**
The system tracks two distinct types of points:
*   **Spendable Points (`PromotionPoint`):** The currency the user can actually spend.
*   **Tier Points (`CurrentYearTierPoints`):** A lifetime/annual tracking metric used strictly for evaluating VIP tier upgrades. Spending points does *not* reduce Tier Points.

**Earning Points:**
Points are awarded based on the final price of services rendered.
*   **Business Rule:** Points are **ONLY** awarded when the staff updates a booking status to `Completed`.
*   **FE Action:** Do not show points as "earned" or "pending" while a booking is in `Pending` or `CheckedIn` status. Only reflect points after the booking lifecycle concludes.

**Spending Points:**
Users have two avenues to spend their `PromotionPoint` balance:
1.  **Direct Booking Discount:** Applying points directly in the `CreateBookingDTO` to reduce the final fiat cost.
2.  **Voucher Redemption:** Exchanging points for a reusable discount code via `POST /api/v1/vouchers/redeem`.

> **Note: FIFO Expiration Rule**
> The backend enforces a First-In, First-Out (FIFO) logic for point expiration. Points earned oldest are spent or expired first.

### 3. Ranking & Tier Pipeline

The Tier system (e.g., Standard, Gold, Platinum) unlocks benefits like VIP-only Time Slots and point multipliers.

**Evaluation Logic:**
A user's Tier upgrade is evaluated automatically when a booking is completed.
*   **Business Rule:** Upgrades are evaluated strictly against `CurrentYearTierPoints` (total accumulated) checking if it meets a Tier's `MinAccumulatedPoints`. It does NOT check the user's current spendable balance. A user who spends all their points can still reach Platinum status.

**Annual Reset Worker:**
*   **Business Rule:** The system utilizes an `AnnualTierResetWorker` which triggers every year on January 1st. This worker resets `CurrentYearTierPoints` and evaluates if the user maintains their current tier or downgrades based on the previous year's activity.

**FE Actionable (User Retention UI):**
Because tiers and certain points reset annually, the Frontend plays a vital role in user engagement.
*   **Implementation Requirement:** The Frontend should read the expiration and tier data to display proactive UI warnings. For example, during December, display banners such as: *"Your Gold status and 500 points expire on Dec 31st! Book a wash now to maintain your benefits!"*

---
*End of Document*
