# Product Specification: Studio Booking Marketplace Platform

> **Status**: Living Domain Specification  
> **Currency**: Saudi Riyal (`SAR`), formatted to 2 decimal places with `MidpointRounding.AwayFromZero`  
> **Timezone**: Arabia Standard Time (`AST`, `UTC+3`), stored as `DateTimeOffset` in UTC with AST presentation  
> **UI Direction**: Arabic (RTL-first)

---

## 1. Goal

Build a robust, multi-sided marketplace web platform connecting creative studio owners with clients seeking rentable studio space (photography, videography, podcasting, audio recording, and rehearsal rooms) in the Saudi/Arabic market.

The platform provides seamless discovery, transparent hourly pricing, instant booking secured by a 20% upfront deposit in SAR, database-level concurrency protection against double-booking, automated cancellation/refund rules, in-app notifications and double-entry ledger event logs, and admin-supervised studio onboarding and dispute arbitration. All business rules reside strictly in the **Domain Layer** under Clean Architecture principles.

---

## 2. User Roles & Permissions

### 2.1. Customer
- **Discovery**: Search and filter approved studios and active spaces by city, category, hourly rate, and capacity.
- **Booking**: Select 1-hour slots matching or exceeding the space's minimum booking duration within daily operating hours.
- **Deposit Payment**: Instantly secure booking by paying a 20% deposit in SAR.
- **Hold Duration**: Booking initiates in `PendingPayment` with a **15-minute hold**; failure or timeout transitions to `PaymentFailed` and releases the slot.
- **Cancellation**: Cancel with full deposit refund if $\ge 24$ hours before session start; non-refundable if $< 24$ hours before start. Cancellation is strictly prohibited once the session has started (`now < Start`).
- **Disputes**: Open a dispute within **48 hours after session end time** (`TimeRange.End`).
- **Reviews**: Submit a 1–5 star rating and written review after completing a booking. Reviews are **strictly prohibited** if a booking has been disputed or resolved via arbitration (`DisputeResolved`).

### 2.2. Studio Owner
- **Studio Profile**: Create and edit studio business details (name, description, city, address).
- **Space Management**: Create and manage multiple bookable spaces under their studio (hourly rate, minimum booking duration, equipment, capacity, daily operating hours).
- **Owner-Only Authorization**: Studio owners can only view, edit, or delete their own studios, spaces, and bookings.
- **Moderation Workflow**: Submit studio profile for Admin review.
- **Bookings Dashboard**: View incoming, confirmed, completed, and cancelled bookings.
- **Owner Cancellation**: Cancel an existing booking prior to session start (`now < Start`), triggering an immediate 100% refund of deposit to customer.
- **Disputes**: Open a dispute within **48 hours after session end time** (`TimeRange.End`) for customer damage, overstay, or no-shows.
- **Payouts**: Receive released net deposit payout after the 48-hour dispute window has elapsed without dispute, tracked in owner ledger balance.

### 2.3. Admin
- **Studio Approval Queue**: Review pending studio submissions; approve or reject with written reason.
- **Platform Overview**: Monitor all platform studios, spaces, bookings, and financial metrics.
- **Dispute Arbitration**: Arbitrate disputes within the platform and issue binding financial resolution using the platform dispute math formula.
- **Governance**: Suspend violating studios or spaces.

---

## 3. State-Transition Tables

### 3.1. Studio State Machine

| Current State | Trigger / Event | Guard / Condition | Next State | Side Effects |
|---|---|---|---|---|
| `Draft` | `SubmitForApproval` | Has at least 1 valid space defined | `PendingApproval` | Placed in Admin review queue |
| `PendingApproval` | `Approve` (Admin) | Admin approval action | `Approved` | Studio and active spaces go live on marketplace |
| `PendingApproval` | `Reject` (Admin) | Rejection reason provided | `Rejected` | Owner notified with reason; can edit and resubmit |
| `Rejected` | `SubmitForApproval` | Owner updates profile/spaces | `PendingApproval` | Re-enters Admin review queue |
| `Approved` | `Suspend` (Admin) | Policy breach or dispute investigation | `Suspended` | Hidden from public search; existing bookings preserved |
| `Suspended` | `Reactivate` (Admin) | Issue resolved | `Approved` | Restored to public search |

### 3.2. Booking State Machine

| Current State | Trigger / Event | Guard / Condition | Next State | Side Effects |
|---|---|---|---|---|
| *None* | `Create` (Customer) | Valid duration $\ge$ min hours; within same-day operating hours; no slot overlap; valid price snapshot | `PendingPayment` | Stale `PendingPayment` holds for this space expired in same transaction; slot reserved with 15-minute hold timer |
| `PendingPayment` | `ConfirmDepositPayment` | Payment gateway confirmation within 15 min (`now <= CreatedAt + 15m`) | `Confirmed` | Slot permanently confirmed; owner notified; escrow credited |
| `PendingPayment` | `ExpireHold` / `FailPayment` | 15 minutes elapsed without payment (`now > CreatedAt + 15m`) or payment failed | `PaymentFailed` | Slot released; exclusion constraint freed |
| `Confirmed` | `CancelByCustomer` | $\text{now} \le \text{Start} - 24\text{h}$ AND $\text{now} < \text{Start}$ | `Cancelled` | 100% deposit refunded to customer; 0 payout to owner; 0 platform commission; slot released |
| `Confirmed` | `CancelByCustomer` | $\text{Start} - 24\text{h} < \text{now} < \text{Start}$ | `Cancelled` | 0% refund to customer; owner credited net payout ($\text{Deposit} - \text{Commission}$); slot released |
| `Confirmed` | `CancelByCustomer` | $\text{now} \ge \text{Start}$ | *Rejected* | Domain error: Customer cancellation is not allowed once session has started |
| `Confirmed` | `CancelByOwner` | $\text{now} < \text{Start}$ | `CancelledByOwner` | 100% deposit refunded to customer; slot released |
| `Confirmed` | `AutoComplete` | $\text{now} \ge \text{TimeRange.End}$ | `Completed` | Booking marked complete; unlocks 48-hour dispute window and review eligibility |
| `Completed` | `ReleaseOwnerPayout` | $\text{now} \ge \text{TimeRange.End} + 48\text{h}$ AND no dispute raised | `Completed` (Payout Released) | Payout released; owner ledger credited with `OwnerNet`; platform commission recognized |
| `Confirmed` or `Completed` | `RaiseDispute` (Customer or Owner) | $\text{now} \le \text{TimeRange.End} + 48\text{h}$ | `Disputed` | Payout hold placed; Admin dispute ticket opened; review eligibility revoked |
| `Disputed` | `ResolveDispute` (Admin) | Valid resolution per dispute math: $\text{CustomerRefund} + \text{OwnerNet} + \text{Commission} == \text{Deposit}$ | `DisputeResolved` | Financial settlement executed via ledger; review eligibility permanently revoked |

---

## 4. Business Rules (Domain Layer Invariants)

All business rules listed below must reside exclusively inside the **Domain Layer** (Entities, Value Objects, and Domain Services) and must be thoroughly unit tested with xUnit.

### 4.0. Domain Clock Rule: Domain Never Reads the Clock
1. **Purity & Isolation**:
   The **Domain Layer never reads the system clock directly**. Direct calls to `DateTimeOffset.UtcNow`, `DateTime.UtcNow`, `DateTime.Now`, or system timers are strictly forbidden inside `StudioBooking.Domain`.
2. **Explicit Parameter Passing**:
   Every domain entity method, value object method, or domain service requiring the current time must accept `DateTimeOffset now` as an explicit parameter:
   - `booking.CancelByCustomer(DateTimeOffset now)`
   - `booking.CancelByOwner(DateTimeOffset now)`
   - `booking.ExpireHold(DateTimeOffset now)`
   - `booking.IsHoldExpired(DateTimeOffset now)`
   - `booking.AutoComplete(DateTimeOffset now)`
   - `booking.CanRaiseDispute(DateTimeOffset now)`
   - `booking.IsPayoutEligible(DateTimeOffset now)`
3. **Application Layer Clock Injection**:
   The Application Layer obtains `now` via an injectable `TimeProvider` (or `IDateTimeProvider`) and passes it into domain methods.
4. **Testability Invariant**:
   All time-sensitive domain logic must be 100% deterministic and testable without clock mocking or sleep delays.

### 4.1. Double-Booking Prevention & Concurrency Protection
1. **PostgreSQL Extension & Range Type**:
   PostgreSQL `btree_gist` extension is enabled to support multicolumn exclusion constraints with equality (`=`) and timestamp with time zone range overlap (`&&`):
   ```sql
   CREATE EXTENSION IF NOT EXISTS btree_gist;
   ```
2. **PostgreSQL Exclusion Constraint (`tstzrange`)**:
   Timestamps are stored as UTC `DateTimeOffset` (`timestamptz`). Active bookings on the same space must not overlap, enforced via `tstzrange` (not `tsrange`):
   ```sql
   ALTER TABLE "Bookings"
   ADD CONSTRAINT "no_overlapping_active_bookings"
   EXCLUDE USING gist (
       "SpaceId" WITH =,
       tstzrange("StartTime", "EndTime", '[)') WITH &&
   )
   WHERE ("Status" IN ('PendingPayment', 'Confirmed'));
   ```
3. **EF Core Column Conversion**:
   `BookingStatus` enum must be stored in the database as a string (`varchar`) using:
   ```csharp
   builder.Property(b => b.Status)
          .HasConversion<string>()
          .HasMaxLength(50);
   ```
   This ensures PostgreSQL evaluates `"Status" IN ('PendingPayment', 'Confirmed')` directly against string values, eliminating any mismatch between EF Core integer enum representations and SQL constraint predicates.
4. **Atomic Stale Hold Expiration in the Same Transaction**:
   Before inserting any new booking hold, the application must execute an atomic sweep on that specific space:
   ```sql
   UPDATE "Bookings"
   SET "Status" = 'PaymentFailed'
   WHERE "SpaceId" = @SpaceId
     AND "Status" = 'PendingPayment'
     AND "CreatedAt" + INTERVAL '15 minutes' < @Now;
   ```
   This query runs inside the **same database transaction** immediately prior to inserting the new `PendingPayment` record. This guarantees that expired holds never block new reservations or cause false positive exclusion constraint conflicts.
   - **Test Requirement**: An automated unit and integration test must verify that an expired `PendingPayment` hold on Space X does not block a new booking for the same slot when inserted, and that the expired hold transitions to `PaymentFailed` within the transaction.
5. **Conflict Handling**:
   Application & API layers catch database exclusion constraint violations (`PostgresException` with code `23P01`) and map them directly to **HTTP 409 Conflict** with message: `"This time slot has already been reserved."`
6. **Concurrency Test Requirement**:
   An automated integration test must simulate concurrent booking attempts for the exact same slot and verify that exactly one succeeds and the other receives a 409 Conflict.

### 4.2. Money Value Object & Rounding Invariant
1. Currency is strictly Saudi Riyal (`SAR`).
2. Precision is 2 decimal places.
3. All calculations involving percentages, division, or rounding **must use `MidpointRounding.AwayFromZero`**:
   $$\text{Round}(value, 2, \text{MidpointRounding.AwayFromZero})$$
4. Boundary rounding behavior must be verified with dedicated unit tests (e.g. `0.005` rounds to `0.01`).

### 4.3. Price Snapshotting at Booking Creation
1. When a booking is created, it captures an immutable snapshot of:
   - `HourlyRateSnapshot`: `Space.HourlyRate` at the moment of booking.
   - `SpaceNameSnapshot`: `Space.Name` at the moment of booking.
2. Subsequent price modifications or renaming by the Studio Owner will not affect existing active, confirmed, or historical bookings.

### 4.4. Financial Calculations, Dispute Math & Split Formula
1. **Total Amount**:
   $$\text{TotalAmount} = \text{HourlyRateSnapshot} \times \text{DurationHours}$$
2. **Deposit (20%)**:
   $$\text{DepositAmount} = \text{Round}(\text{TotalAmount} \times 0.20, 2, \text{MidpointRounding.AwayFromZero})$$
3. **Remaining On-Site Balance (80%)**:
   $$\text{RemainingBalance} = \text{TotalAmount} - \text{DepositAmount}$$
4. **Standard Platform Commission (10% of Deposit)**:
   $$\text{PlatformCommission} = \text{Round}(\text{DepositAmount} \times 0.10, 2, \text{MidpointRounding.AwayFromZero})$$
5. **Standard Owner Payout**:
   $$\text{OwnerPayoutAmount} = \text{DepositAmount} - \text{PlatformCommission}$$
6. **Dispute Mathematical Model**:
   When an Admin resolves a dispute with an approved `CustomerRefund` ($0 \le \text{CustomerRefund} \le \text{DepositAmount}$), the remaining deposit constitutes owner gross earnings, from which platform commission is deducted:
   $$\text{OwnerGross} = \text{DepositAmount} - \text{CustomerRefund}$$
   $$\text{Commission} = \text{Round}(\text{OwnerGross} \times 0.10, 2, \text{MidpointRounding.AwayFromZero})$$
   $$\text{OwnerNet} = \text{OwnerGross} - \text{Commission}$$
   - **Strict Equality Invariant**:
     $$\text{CustomerRefund} + \text{OwnerNet} + \text{Commission} == \text{DepositAmount}$$
     Because $\text{OwnerNet} = \text{OwnerGross} - \text{Commission}$, substituting yields:
     $$\text{CustomerRefund} + (\text{OwnerGross} - \text{Commission}) + \text{Commission} = \text{CustomerRefund} + \text{OwnerGross} = \text{DepositAmount}$$
     This mathematical identity guarantees that the sum of allocations always strictly equals `DepositAmount` with zero penny leakage or rounding drift.
   - **Equivalence with Standard (Non-Dispute) Formula**:
     - **When CustomerRefund = 0 SAR**:
       $$\text{OwnerGross} = \text{DepositAmount}$$
       $$\text{Commission} = \text{Round}(\text{DepositAmount} \times 0.10, 2, \text{MidpointRounding.AwayFromZero})$$
       $$\text{OwnerNet} = \text{DepositAmount} - \text{Commission}$$
       This matches the standard non-dispute payout formula identically.
     - **When CustomerRefund = DepositAmount (100% Customer Refund)**:
       $$\text{OwnerGross} = 0 \text{ SAR}$$
       $$\text{Commission} = \text{Round}(0 \times 0.10) = 0 \text{ SAR}$$
       $$\text{OwnerNet} = 0 - 0 = 0 \text{ SAR}$$
       The platform fee is waived, the owner receives 0, and the customer receives 100%.
   - **Mandatory Test Cases for Dispute Math**:
     - *Case 1 (Zero Refund / Standard Parity)*: Deposit = 200.00 SAR, CustomerRefund = 0.00 SAR $\implies$ OwnerGross = 200.00 SAR, Commission = 20.00 SAR, OwnerNet = 180.00 SAR. Sum = 200.00 SAR.
     - *Case 2 (Full Refund)*: Deposit = 200.00 SAR, CustomerRefund = 200.00 SAR $\implies$ OwnerGross = 0.00 SAR, Commission = 0.00 SAR, OwnerNet = 0.00 SAR. Sum = 200.00 SAR.
     - *Case 3 (Partial Split)*: Deposit = 100.00 SAR, CustomerRefund = 40.00 SAR $\implies$ OwnerGross = 60.00 SAR, Commission = 6.00 SAR, OwnerNet = 54.00 SAR. Sum = 100.00 SAR.
     - *Case 4 (Fractional Rounding with AwayFromZero)*: Deposit = 105.50 SAR, CustomerRefund = 30.25 SAR $\implies$ OwnerGross = 75.25 SAR, Commission = Round(7.525, 2) = 7.53 SAR, OwnerNet = 75.25 - 7.53 = 67.72 SAR. Sum: 30.25 + 67.72 + 7.53 = 105.50 SAR.

### 4.5. Owner Payout Release & Ledger Accounting
1. **Payout Release Window**:
   Owner payouts are held in platform escrow during the session and throughout the **48-hour post-session dispute window**.
   - If no dispute is lodged within 48 hours after session end ($\text{now} \ge \text{TimeRange.End} + 48\text{h}$), the booking payout becomes eligible for release.
   - If a dispute is raised within 48 hours, payout release is frozen until Admin resolution (`DisputeResolved`), at which point `OwnerNet` is released per the dispute resolution formula.
2. **Double-Entry Append-Only Ledger**:
   All financial movements are recorded in an immutable `LedgerEntries` table (`Id`, `BookingId`, `EntryType`, `DebitAccount`, `CreditAccount`, `Amount`, `CreatedAt`).
   - **Step 1: Deposit Payment Confirmation**:
     - Debit: `CustomerPaymentGateway` (Asset) `+DepositAmount`
     - Credit: `PlatformEscrowCustomerHeld` (Liability) `+DepositAmount`
   - **Step 2A: Standard Payout Release (After 48h No-Dispute)**:
     - Debit: `PlatformEscrowCustomerHeld` (Liability) `-DepositAmount`
     - Credit: `PlatformCommissionRevenue` (Revenue) `+Commission`
     - Credit: `StudioOwnerPayable` (Liability / Owner Balance) `+OwnerNet`
     - Booking marked as `PayoutReleased` with `PayoutReleasedAt = now`.
   - **Step 2B: Customer Cancellation >= 24h (Full Refund)**:
     - Debit: `PlatformEscrowCustomerHeld` (Liability) `-DepositAmount`
     - Credit: `CustomerRefundPayable` (Liability) `+DepositAmount`
   - **Step 2C: Customer Cancellation < 24h (Non-Refundable)**:
     - Debit: `PlatformEscrowCustomerHeld` (Liability) `-DepositAmount`
     - Credit: `PlatformCommissionRevenue` (Revenue) `+Commission`
     - Credit: `StudioOwnerPayable` (Liability) `+OwnerNet`
   - **Step 2D: Dispute Settlement**:
     - Debit: `PlatformEscrowCustomerHeld` (Liability) `-DepositAmount`
     - Credit: `CustomerRefundPayable` (Liability) `+CustomerRefund`
     - Credit: `PlatformCommissionRevenue` (Revenue) `+Commission`
     - Credit: `StudioOwnerPayable` (Liability) `+OwnerNet`
3. **Owner Balance Calculation**:
   The studio owner's available payout balance is calculated dynamically from the ledger as $\sum \text{Credit} - \sum \text{Debit}$ on `StudioOwnerPayable` for their `OwnerId`.

### 4.6. Hold Duration & Auto-Completion
1. **Pending Payment Hold**: A new booking reserves the slot for **15 minutes**. A background worker or on-query sweep expires reservations exceeding 15 minutes (`now > CreatedAt + 15m`) to `PaymentFailed`.
2. **Auto-Completion**: Once `now >= TimeRange.End`, a `Confirmed` booking automatically transitions to `Completed`.

### 4.7. Customer Cancellation Invariants
1. **Prior to Session Start**: Customer cancellation requires `now < Start`. If `now >= Start`, `CancelByCustomer` is rejected with domain error.
2. **Refund Rules**:
   - If $\text{now} \le \text{Start} - 24\text{h}$: Status $\to$ `Cancelled`, 100% deposit refunded to customer.
   - If $\text{Start} - 24\text{h} < \text{now} < \text{Start}$: Status $\to$ `Cancelled`, 0% refund (non-refundable deposit credited to owner net payout after commission).

### 4.8. Dispute Handling
1. **Parties**: Either the `Customer` or `StudioOwner` associated with the booking can open a dispute.
2. **Window**: Must be submitted within **48 hours after `TimeRange.End`** (`now <= TimeRange.End + 48h`).
3. **Tracking**: The dispute entity records `RaisedByUserId`, `RaisedByRole`, `Reason`, and `CreatedAt`.
4. **Outcome**: After Admin arbitration, booking status transitions to `DisputeResolved`, payout is released per the dispute formula, and review submission is blocked.

### 4.9. Owner-Only Authorization
1. Studio Owners are authorized to modify **only** their own studios, spaces, and bookings.
2. Every command verifying ownership must execute an explicit domain/application guard:
   $$\text{Studio.OwnerId} == \text{CurrentUserId} \quad \text{or} \quad \text{CurrentUser.IsAdmin}$$
3. Unauthorized attempts must throw a domain/application `ForbiddenException` returning **HTTP 403 Forbidden**, backed by unit and integration tests.

### 4.10. Reviews & Ratings
1. Only customers with a `Completed` booking can submit a review.
2. **Review Eligibility After DisputeResolved = Not Allowed**:
   - If a booking has been disputed (transitioned to `Disputed` or `DisputeResolved`), review submission is **strictly prohibited**.
   - `booking.CanSubmitReview()` returns `true` only if `Status == BookingStatus.Completed` and no dispute was ever raised (`Dispute == null`).
   - Attempting to submit a review for a booking with status `DisputeResolved` throws a domain invariant exception (`InvalidOperationException`).
   - This prevents retaliatory or compromised reviews resulting from arbitration conflicts.
3. Exactly one review per booking.
4. Rating must be an integer: $1 \le \text{Rating} \le 5$.

### 4.11. Space Operating Hours & Timezone
1. **Platform Timezone**: All scheduling, calculations, and boundaries operate in **Arabia Standard Time (`AST`, `UTC+3`)**. Stored in database as UTC `DateTimeOffset`.
2. **Operating Hours**: Each Space specifies daily `OpeningTime` and `ClosingTime` (e.g. `09:00` to `23:00` AST).
3. **No Overnight (Cross-Midnight) Operating Hours in MVP**:
   - Operating hours are strictly restricted to the **same calendar day**:
     $$\text{OpeningTime} < \text{ClosingTime}$$
   - Cross-midnight hours (e.g., `20:00` to `04:00` next morning) are **explicitly disallowed in the MVP** and fail domain validation with `InvalidOperatingHoursException`.
4. **Booking Guard**: Any requested `TimeRange` must fall completely within the space's daily operating hours on the same calendar day:
   - $\text{StartTime.Date} == \text{EndTime.Date}$
   - $\text{StartTime.TimeOfDay} \ge \text{OpeningTime}$
   - $\text{EndTime.TimeOfDay} \le \text{ClosingTime}$
   Bookings spanning across closing time, starting before opening time, or crossing midnight are rejected with a domain validation error.

### 4.12. Notifications & Event Audit Trail
1. **In-App Notification Center**: Significant lifecycle events (Studio Approved/Rejected, Booking Confirmed, Hold Expired, Cancellation, Dispute Raised/Resolved, Payout Released) generate in-app notification records for affected users (`UserId`, `Title`, `BodyAr`, `IsRead`, `CreatedAt`).
2. **Extensibility**: Backed by domain events and an `INotificationService` interface, allowing plug-and-play email or SMS dispatchers in the future without modifying core business logic.

---

## 5. Out of Scope (MVP)

1. Live credit card processor credentials (Stripe / Moyasar) — simulated mock gateway used for test/dev.
2. Multi-currency conversions (SAR only).
3. Dynamic / peak-hour pricing rules.
4. Direct in-app live chat (disputes use structured claims & admin notes).
5. Native mobile applications (Arabic RTL-first Responsive Web App).
6. Automated bank wire transfers for payouts (tracked via ledger balance).
7. Overnight (cross-midnight) space operating hours (all operating hours and bookings must begin and end within the same calendar day).
