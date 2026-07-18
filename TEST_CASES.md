# SmartWash System - Test Cases

## Test Environments
- **Backend / API:** .NET 8 (Tested via Swagger UI and Postman)
- **Database:** MySQL
- **Frontend Web / Admin Portal:** Chrome, Safari, Firefox, Edge
- **Mobile Application:** iOS, Android
- **Hardware Integrations:** Edge Camera (for LPR/OCR)

## Test Cases

| No | Function Name | Sheet Name | Description | Pre-Condition |
|----|---------------|------------|-------------|---------------|
| 1 | Customer Registration | Authentication & User | Register a new customer account in the system. | User does not have an existing active account with the same email/phone. |
| 2 | User Login | Authentication & User | Log into the system and retrieve an Access Token. | Valid registered account credentials. |
| 3 | OTP Verification & Resend | Authentication & User | Verify OTP code and request resending OTP. | User has requested an action requiring OTP (e.g., login, registration). |
| 4 | Password Recovery | Authentication & User | Forgot password flow and reset to a new password. | Valid account with access to registered email/phone. |
| 5 | Token Refresh & Logout | Authentication & User | Refresh an expired access token and securely log out. | Valid refresh token / Active login session. |
| 6 | User Profile Management | Authentication & User | View, update, or delete the user account profile. | Logged-in user. |
| 7 | E-Wallet Balance Inquiry | Authentication & User | Look up personal E-wallet balance. | Logged-in user with an initialized wallet. |
| 8 | E-Wallet Top-up & Callback | Authentication & User | Top up E-wallet and process successful payment callback. | Logged-in user; Payment gateway is operational. |
| 9 | Time Slot Availability & AI Suggestion | B2C Booking | Fetch available time slots and receive AI-suggested time slots. | Branch and service are selected. |
| 10 | Create App Booking | B2C Booking | Book a service appointment via the customer application. | Valid selected time slot, service, and vehicle. |
| 11 | Create Walk-in Booking | B2C Booking | Create a booking directly at the service counter (walk-in). | Staff/Manager account logged in at the counter. |
| 12 | Booking Modification & Cancellation | B2C Booking | Cancel, reschedule, or modify vehicle conditions for an appointment. | Existing active booking in Pending state. |
| 13 | Booking History & Details | B2C Booking | Look up the history and view details of personal bookings. | Logged-in user with past or active bookings. |
| 14 | Booking Payment Link & Status | B2C Booking | Generate payment link and check the payment status of a booking. | Existing unpaid booking. |
| 15 | Service & Vehicle Compatibility Check | B2C Booking | Check if the selected service is compatible with the vehicle type. | Selected service and vehicle. |
| 16 | B2B Partner Registration | Business B2B | Register a B2B business partner account. | No existing B2B account with the same business details. |
| 17 | B2B Profile Management | Business B2B | View and manage personal B2B business profile. | Logged-in B2B partner. |
| 18 | B2B Registration Approval | Business B2B | Admin reviews and approves/rejects B2B registration requests. | Pending B2B registration request; Admin account. |
| 19 | B2B Bulk Service Booking | Business B2B | Enterprise books services for multiple vehicles in batch. | Approved B2B account with registered fleet. |
| 20 | B2B Appointment Management | Business B2B | Enterprise views history, reschedules, or cancels appointments. | Existing B2B bookings. |
| 21 | B2B Vehicle & Service Status Lookup | Business B2B | Enterprise looks up vehicle list and their current service status. | Approved B2B account. |
| 22 | B2B Dashboard Tracking | Business B2B | Enterprise views overview dashboard and statistics. | Approved B2B account with booking history. |
| 23 | B2B Invoices & Export | Business B2B | Enterprise views list of invoices and exports statistical files. | B2B account with completed transactions. |
| 24 | B2B Vehicle Lane Pre-assignment | Business B2B | Pre-assign specific lanes and positions for enterprise vehicles. | Active B2B booking; Manager/Admin account. |
| 25 | Fleet CSV Template & Batch Import | Fleet Management | Download CSV template and batch import a list of fleet vehicles. | B2B account; valid CSV file format. |
| 26 | Fleet Import Approval | Fleet Management | Approve or reject the imported fleet vehicle list. | Admin account; pending imported fleet list. |
| 27 | Fleet Vehicle Arrival Check-in | Fleet Management | Record the arrival of a fleet vehicle at the facility. | Staff account; recognized fleet vehicle. |
| 28 | Fleet Service Progress Management | Fleet Management | Manage fleet washing states (start, walk-out, checkout). | Checked-in fleet vehicle. |
| 29 | Fleet Queue & Active Processing Lookup | Fleet Management | View fleet queue and currently processing vehicles. | Active branch with ongoing fleet services. |
| 30 | Fleet Serviced History Lookup | Fleet Management | Look up past serviced history of fleet vehicles. | Fleet vehicles with completed bookings. |
| 31 | Fleet Performance Dashboard | Fleet Management | Track fleet service performance on the dashboard. | Admin/Manager account. |
| 32 | Service Catalog & Pricing View | Services & Vehicles | View detailed list and pricing of car care services. | None. |
| 33 | Personal Vehicle Management | Services & Vehicles | Manage personal vehicle list (add, edit, delete). | Logged-in user. |
| 34 | OCR License Plate Recognition | Services & Vehicles | Automatically recognize vehicle info from license plates (OCR). | Clear image of a license plate; Camera/OCR service active. |
| 35 | Car Brands & Models Catalog View | Services & Vehicles | View system catalog of Car Brands and Models. | None. |
| 36 | System Car Model Management | Services & Vehicles | Admin adds, updates, or deletes system Car Model data. | Admin account. |
| 37 | Customer Car Model Suggestion | Services & Vehicles | Customer submits a suggestion for a new Car Model. | Logged-in user. |
| 38 | Suggested Car Model Approval | Services & Vehicles | Admin approves or rejects customer-suggested Car Models. | Admin account; pending model suggestions. |
| 39 | Reward Points & Transaction History Lookup | Services & Vehicles | Look up personal reward points and transaction history. | Logged-in user. |
| 40 | Branch Management | Admin Tasks | Manage Branch list (add, edit branch info). | Admin account. |
| 41 | Branch Personnel List View | Admin Tasks | View the list of personnel under a specific branch. | Admin or Branch Manager account. |
| 42 | Staff Creation & Inter-branch Transfer | Admin Tasks | Create staff accounts and transfer personnel between branches. | Admin account. |
| 43 | Branch Manager Account Management | Admin Tasks | Manage Branch Manager accounts and their statuses. | Admin account. |
| 44 | Operation Staff Account Management | Admin Tasks | Manage Operation Staff accounts and their statuses. | Admin or Branch Manager account. |
| 45 | Customer Account & Points Sync Management | Admin Tasks | Manage customer accounts and trigger reward points synchronization. | Admin account. |
| 46 | Add New Service | Admin Tasks | Add a new car care service to the system. | Admin account. |
| 47 | Update Service Configuration | Admin Tasks | Update the configuration of an existing car care service. | Admin account; existing service. |
| 48 | Delete Service | Admin Tasks | Soft or hard delete a car care service. | Admin account; existing service. |
| 49 | Inventory & Unit Catalog Management | Admin Tasks | Manage the item catalog and units of measurement for inventory. | Admin account. |
| 50 | Service Lanes Management | Admin Tasks | Manage the system-wide list of Service Lanes. | Admin account. |
| 51 | Add New Vehicle Type | Admin Tasks | Add a new served vehicle classification/type. | Admin account. |
| 52 | Update Vehicle Type | Admin Tasks | Update information of an existing vehicle classification. | Admin account; existing vehicle type. |
| 53 | Delete Vehicle Type | Admin Tasks | Remove a vehicle classification from the system. | Admin account; existing vehicle type. |
| 54 | Service Material Consumption Quota Configuration | Admin Tasks | Configure mandatory material consumption quotas for services. | Admin account; existing services and inventory items. |
| 55 | Exceptional Vehicle Type Approval | Admin Tasks | Approve or reject dynamically created "Other" vehicle types. | Admin account; pending exceptional vehicles. |
| 56 | Branch Revenue Reports Tracking | Admin Tasks | Track and generate branch revenue reports. | Admin account. |
| 57 | Trigger Revenue Growth Campaigns | Admin Tasks | Manually trigger automated revenue growth campaigns. | Admin account. |
| 58 | Personnel Lane Assignment | Manager Tasks | Allocate and assign personnel to specific Service Lanes. | Manager account; available staff and lanes. |
| 59 | Branch Service Lanes Management | Manager Tasks | Initialize, update, or delete Service Lanes at the branch. | Manager account. |
| 60 | Branch TimeSlots Configuration | Manager Tasks | Manage TimeSlots configuration (capacity, availability) for the branch. | Manager account. |
| 61 | Booking Check-in & Task Assignment | Manager Tasks | Receive booking check-ins and manually assign tasks to personnel. | Manager account; customer arrives at branch. |
| 62 | Voucher Proposal Approval | Manager Tasks | Approve or reject revenue stimulus proposals (Vouchers). | Manager account; pending voucher proposals. |
| 63 | Staff Shift Assignment | Manager Tasks | Assign duty schedules and work shifts for staff members. | Manager account. |
| 64 | Staff Overtime Request Approval | Manager Tasks | Approve or reject staff overtime requests. | Manager account; pending overtime requests. |
| 65 | Staff Shift Swap Request Approval | Manager Tasks | Approve or reject staff shift swap requests. | Manager account; pending shift swap requests. |
| 66 | Branch Work Shifts Catalog Management | Manager Tasks | Manage the standard work shifts catalog for the branch. | Manager account. |
| 67 | Tier Configuration Management | Manager Tasks | Setup and edit customer membership Tier configurations. | Manager/Admin account. |
| 68 | Inventory & Shipment Lookup | Inventory Management | Lookup actual inventory quantity and shipment lists. | Admin or Manager account. |
| 69 | Wear & Condition Coefficients Setup | Inventory Management | Set up vehicle condition and material wear coefficients. | Admin or Manager account. |
| 70 | Branch Inventory Level Configuration | Inventory Management | Configure branch inventory settings and minimum stock levels. | Manager account. |
| 71 | Inventory Shipments & Adjustments | Inventory Management | Receive shipments and perform manual quantity adjustments. | Manager account. |
| 72 | Defective/Expired Shipment Disposal | Inventory Management | Mark and dispose of defective or expired material shipments. | Manager account; existing defective stock. |
| 73 | Inventory Transaction History Lookup | Inventory Management | Look up historical inventory transactions and stock movements. | Manager account. |
| 74 | Inventory Profit & Expiration Reports | Inventory Management | View inventory profit reports and alerts for expiring materials. | Manager account. |
| 75 | Staff Material Request Approval | Inventory Management | Approve requests for additional materials triggered by staff. | Manager account; pending staff material requests. |
| 76 | Staff Lane & Task Tracking | Operation Staff | View assigned lane and track incoming tasks during the shift. | Operation Staff account assigned to a shift/lane. |
| 77 | Service Progress & Status Update | Operation Staff | Update progress (check-in, start, complete service). | Operation Staff account; active assigned booking. |
| 78 | Late Booking No-show Reporting | Operation Staff | Report a 'No-show' for a booking that missed its appointment. | Operation Staff account; overdue booking. |
| 79 | Vehicle/Service Discrepancy Reporting | Operation Staff | Report mismatches between actual vehicle/service and registered info. | Operation Staff account; active booking with discrepancy. |
| 80 | Emergency Force Cancellation | Operation Staff | Force cancel a booking at the door due to force majeure/unavailability. | Operation Staff account; active arriving booking. |
| 81 | Additional Material Allocation Request | Operation Staff | Send a request for additional material allocation (over quota). | Operation Staff account; active task requiring more supplies. |
| 82 | Staff Schedule, Swap & Overtime Request | Operation Staff | Look up schedule, request a shift swap, or request overtime. | Operation Staff account. |
| 83 | Counter Voucher Scanning & Consumption | Operation Staff | Scan and consume a voucher code for a customer at the counter. | Operation Staff account; valid voucher code. |
| 84 | Personal Voucher View & Application | Promotions & Extras | Customer views personal vouchers and applies them to a booking. | Logged-in user with owned vouchers. |
| 85 | System Voucher Repository Management | Promotions & Extras | Admin adds, edits, or deletes system vouchers. | Admin account. |
| 86 | Manual Voucher Issuance | Promotions & Extras | Admin explicitly issues or gifts vouchers to user accounts. | Admin account. |
| 87 | Automatic Voucher Campaign Triggers | Promotions & Extras | Process automatic triggers for campaigns (e.g., Birthday, VIP, Weather). | System cron/scheduler or event trigger. |
| 88 | Personal Invoice Lookup & PDF Export | Promotions & Extras | Look up personal invoices and export them as PDF files. | Logged-in user with completed transactions. |
| 89 | Monthly Billing Invoice Generation | Promotions & Extras | Automatically calculate and generate cycle invoices for B2B. | End of billing cycle; Active B2B partners. |
| 90 | PayOS Webhook Processing | Promotions & Extras | Listen to and process callback webhooks from the PayOS payment gateway. | Incoming valid webhook payload from PayOS. |
| 91 | Edge Camera Vehicle & LPR Detection | Promotions & Extras | Edge camera automatically detects vehicle presence and reads plates. | Active Edge Camera integration; vehicle arrives. |
| 92 | AI Chatbot Consulting & Suggestions | Promotions & Extras | Interact with AI Chatbot for consulting and intelligent service suggestions. | Logged-in user; valid AI Chatbot session. |
