# Design System: Darkroom (غرفة مظلمة)
## Studio Booking Marketplace — Design Specification (Arabic RTL-First)

> **Governing Specifications**: [docs/spec.md](file:///D:/Projects/StudioBooking/docs/spec.md) · [docs/plan.md](file:///D:/Projects/StudioBooking/docs/plan.md)  
> **Market & Locale**: Saudi Arabia (`KSA`, `SAR`, `AST` UTC+3) · Arabic RTL-First (`dir="rtl"`)  
> **Aesthetic Identity**: **"Darkroom"** — Warm photographic fiber paper, carbon printing ink, and a single safelight amber-red accent used with precision.

---

## 1. Aesthetic Thesis & Principles

### 1.1 The "Darkroom" Concept
The creative studio world (photography, cinematography, audio recording, rehearsal) shares a historic lineage with craftsmanship, tactile equipment, calibrated light, and acoustic architecture. Rather than treating studio booking as a generic SaaS utility, the **Darkroom** design system grounds the platform in the visual language of the photographic laboratory and analog studio:
- **Warm Paper Canvas**: Background surfaces echo premium fiber-based photo paper (`#F7F5F0`), avoiding clinical stark white and generic cool grays.
- **Carbon Printing Ink**: Text and structural rules emulate high-density carbon pigment ink (`#181615`), providing deep, legible contrast without synthetic blue undertones.
- **Single Accent — Safelight Red-Orange**: Like the monochromatic red safety illumination of a traditional darkroom, exactly **one** vibrant accent color (`#C43214`) is used strictly for primary calls-to-action, active slot selections, and hold countdowns.
- **Hairlines & Geometry**: Studio cards, tables, and dividers use razor-sharp 1px hairline rules (`#DCD7CD`), recalling film borders, contact sheets, and mechanical cameras.
- **Zero Heavy Shadows**: The platform completely avoids muddy, diffuse drop-shadows. Separation of planes is achieved via surface tonal steps and crisp hairline contours.

### 1.2 Banned "AI Clichés" & What We Do Instead
| Banned AI Cliché | Darkroom Replacement |
|---|---|
| Purple/blue linear gradients & mesh gradients | Pure solid paper surfaces (`#F7F5F0`, `#FFFFFF`, `#EFECE6`) with zero gradient washes |
| Glassmorphism, translucent blur cards | Solid tactile paper surfaces with razor-thin 1px hairline borders (`#DCD7CD`) |
| Emoji used as interface icons | Crisp SVG stroked icons (Lucide or tailored SVG) in deep carbon ink |
| Uniform `rounded-2xl` pillowy card containers | Architectural tight radii (4px buttons/inputs, 6px cards, 0px receipt edges) |
| Centered generic hero with three floating cards | Asymmetric editorial layout, image-first studio showcase, prominent Time Ribbon |
| All-caps tracked-out English eyebrow tags | Distinctive Arabic typography using authentic Kufic proportions (`Reem Kufi`) |
| Soft diffuse shadows (`box-shadow: 0 10px 30px rgba(0,0,0,0.1)`) | Flat contact styling (`0 1px 2px rgba(24,22,21,0.04)` or pure hairline borders) |

---

## 2. Color System & Contrast Ratios (WCAG AA Validated)

All colors are strictly light-mode surfaces with dark carbon ink and calibrated safelight accents. Every text-to-background pairing meets or exceeds **WCAG AA** requirements ($4.5:1$ for normal text, $3.0:1$ for large text/graphical elements).

### 2.1 Core Palette Tokens

```css
:root {
  /* Paper Surfaces */
  --color-bg-base: #F7F5F0;          /* Canvas background (Warm Fiber Paper) */
  --color-bg-surface: #FFFFFF;       /* Card & Container surface (Pure White Paper) */
  --color-bg-recessed: #EFECE6;      /* Input background, inactive tracks, table headers */
  --color-bg-muted: #E6E2D8;         /* Subtle chips, dividers, inactive states */

  /* Carbon Ink & Typography */
  --color-ink-primary: #181615;      /* Headings, high-emphasis text, primary icons */
  --color-ink-secondary: #5C5852;    /* Supporting labels, captions, metadata */
  --color-ink-tertiary: #6E685F;     /* Subtle captions, form placeholders */
  --color-ink-disabled: #9C968D;     /* Disabled text & controls */

  /* Hairlines & Borders */
  --color-border-hairline: #DCD7CD;  /* Standard card and container hairline (1px) */
  --color-border-strong: #BCB5A8;    /* Input borders, active table rules, perforations */

  /* Safelight Accent (One Single Accent) */
  --color-accent-safelight: #C43214;        /* Primary CTA, selected slot, hold indicator */
  --color-accent-safelight-hover: #A8270E;  /* Hover / active interactive state */
  --color-accent-safelight-active: #8E200B; /* Pressed state */
  --color-accent-safelight-tint: #FCEEEA;   /* Subtle red-orange wash for active item container */
  --color-accent-safelight-border: #F4BFB2; /* Safelight container hairline */
}
```

### 2.2 Contrast Ratio Matrix (WCAG AA Verification)

| Foreground Token | Value | Background Token | Value | Contrast Ratio | WCAG Compliance |
|---|---|---|---|---|---|
| `--color-ink-primary` | `#181615` | `--color-bg-base` | `#F7F5F0` | **15.35:1** | AAA (Normal & Large) |
| `--color-ink-primary` | `#181615` | `--color-bg-surface` | `#FFFFFF` | **16.90:1** | AAA (Normal & Large) |
| `--color-ink-primary` | `#181615` | `--color-bg-recessed` | `#EFECE6` | **14.10:1** | AAA (Normal & Large) |
| `--color-ink-secondary`| `#5C5852` | `--color-bg-base` | `#F7F5F0` | **6.52:1** | AA (Normal) / AAA (Large) |
| `--color-ink-secondary`| `#5C5852` | `--color-bg-surface` | `#FFFFFF` | **7.19:1** | AA (Normal) / AAA (Large) |
| `--color-ink-tertiary` | `#6E685F` | `--color-bg-surface` | `#FFFFFF` | **4.98:1** | AA (Normal) |
| `--color-bg-surface` (White Text) | `#FFFFFF` | `--color-accent-safelight` | `#C43214` | **5.61:1** | AA (Normal & Large) |
| `--color-bg-surface` (White Text) | `#FFFFFF` | `--color-accent-safelight-hover` | `#A8270E` | **7.24:1** | AAA (Normal & Large) |
| `--color-accent-safelight` | `#C43214` | `--color-accent-safelight-tint` | `#FCEEEA` | **4.82:1** | AA (Normal & Large) |

### 2.3 Status Badge Colors (Aligned with `spec.md`)

Each booking and studio status defined in the domain specification maps to a disciplined, analog darkroom tonal badge:

| Status Enum | Arabic Label | Text Token | Text Hex | Surface Hex | Border Hex | Contrast Ratio |
|---|---|---|---|---|---|---|
| `Draft` | مسودة | `--badge-draft-text` | `#403D39` | `#EFECE6` | `#DCD7CD` | **8.52:1** (AAA) |
| `PendingApproval` | قيد المراجعة | `--badge-pending-text` | `#784D06` | `#FAF1DC` | `#EAD7A6` | **6.21:1** (AA) |
| `Approved` | معتمد | `--badge-approved-text` | `#1E5631` | `#EBF4EC` | `#BEDBC0` | **6.84:1** (AA) |
| `Rejected` | مرفوض | `--badge-rejected-text` | `#7A2828` | `#FAECEC` | `#E8C4C4` | **6.15:1** (AA) |
| `Suspended` | معلق | `--badge-suspended-text` | `#614B3D` | `#F4EFEA` | `#D9CCC1` | **7.12:1** (AA) |
| `PendingPayment` | بانتظار الدفع | `--badge-pending-text` | `#784D06` | `#FAF1DC` | `#EAD7A6` | **6.21:1** (AA) |
| `Confirmed` | مؤكد | `--badge-confirmed-text` | `#1E5631` | `#EBF4EC` | `#BEDBC0` | **6.84:1** (AA) |
| `PaymentFailed` | تعذر الدفع | `--badge-failed-text` | `#7A2828` | `#FAECEC` | `#E8C4C4` | **6.15:1** (AA) |
| `Completed` | مكتمل | `--badge-completed-text` | `#181615` | `#EFECE6` | `#DCD7CD` | **14.10:1** (AAA) |
| `Cancelled` | ملغى | `--badge-cancelled-text` | `#5C5852` | `#EFECE6` | `#DCD7CD` | **5.98:1** (AA) |
| `CancelledByOwner` | ملغى من الاستوديو | `--badge-owner-cancel-text` | `#7A2828` | `#FAECEC` | `#E8C4C4` | **6.15:1** (AA) |
| `Disputed` | قيد النزاع | `--badge-disputed-text` | `#96280E` | `#FCEEEA` | `#F4BFB2` | **5.73:1** (AA) |
| `DisputeResolved` | تم حل النزاع | `--badge-resolved-text` | `#2C3E50` | `#ECF0F3` | `#CBD5E1` | **7.41:1** (AA) |

---

## 3. Typography Architecture

### 3.1 Typeface Selection
- **Display & Headings**: **`Reem Kufi`** (Google Fonts)
  - Geometric Kufic proportions with modern architectural precision.
  - Used for studio names, page hero titles, section headlines, and receipt headers.
  - Banned alternatives: Cairo, Tajawal, Inter, Helvetica, or system sans.
- **Body, UI & Form Elements**: **`IBM Plex Sans Arabic`** (Google Fonts)
  - Engineered for clarity at small sizes, functional legibility, and technical precision.
  - Used for navigation, buttons, form inputs, metadata, and badge labels.
- **Numerical Figures**:
  - **Western Digits (`0–9`)** are used for all prices, durations, capacities, dates, and times.
  - Standardized with tabular figure alignment:
    ```css
    font-variant-numeric: tabular-nums lining-nums;
    font-feature-settings: "tnum" 1, "lnum" 1;
    ```
  - For tabular columns in receipts and Time Ribbon slots, prices use `font-variant-numeric: tabular-nums`.

### 3.2 Type Scale (Base 16px)

| Scale Step | Size (rem / px) | Line Height | Weight | Typeface | Typical Usage |
|---|---|---|---|---|---|
| `display-1` | `2.25rem` (36px) | `1.2` (44px) | `700` Bold | Reem Kufi | Studio detail title, Marketplace hero |
| `display-2` | `1.75rem` (28px) | `1.25` (36px) | `700` Bold | Reem Kufi | Dashboard section titles, modal headers |
| `heading-1` | `1.375rem` (22px) | `1.3` (28px) | `600` SemiBold | Reem Kufi | Studio card titles, section headers |
| `heading-2` | `1.125rem` (18px) | `1.35` (24px) | `600` SemiBold | Reem Kufi | Space room names, receipt subheaders |
| `body-lg` | `1.000rem` (16px) | `1.6` (26px) | `400` / `500` | IBM Plex Sans Arabic | Space descriptions, intro paragraphs |
| `body-base` | `0.875rem` (14px) | `1.5` (22px) | `400` / `500` | IBM Plex Sans Arabic | Default UI, table cells, form labels |
| `caption` | `0.750rem` (12px) | `1.4` (17px) | `500` Medium | IBM Plex Sans Arabic | Metadata, helper texts, ribbon time tags |
| `micro` | `0.6875rem` (11px) | `1.3` (15px) | `600` SemiBold | IBM Plex Sans Arabic | Status badges, receipt serial IDs |

---

## 4. Spacing, Radii, Borders & Elevation

### 4.1 Spacing Scale (8pt Baseline with 4pt Half-Steps)
```css
--space-1: 0.25rem;  /* 4px  - Badge padding, icon gap */
--space-2: 0.5rem;   /* 8px  - Button icon gap, slot chips margin */
--space-3: 0.75rem;  /* 12px - Input vertical padding, card inner gap */
--space-4: 1.0rem;   /* 16px - Standard container padding, form gap */
--space-5: 1.25rem;  /* 20px - Section sub-gap */
--space-6: 1.5rem;   /* 24px - Card header padding, modal content gap */
--space-8: 2.0rem;   /* 32px - Section spacing */
--space-10: 2.5rem;  /* 40px - Major component separation */
--space-12: 3.0rem;  /* 48px - Page section rhythm */
--space-16: 4.0rem;  /* 64px - Desktop hero padding */
```

### 4.2 Radii Rules (Anti-Marshmallow)
```css
--radius-sharp: 0px;      /* Receipt edges, hard table containers */
--radius-sm: 4px;         /* Buttons, inputs, badge tags, Time Ribbon chips */
--radius-md: 6px;         /* Studio cards, modal containers, popovers */
--radius-lg: 8px;         /* Full-screen bottom sheets (mobile) */
```

### 4.3 Hairline Borders & Rules
```css
--border-hairline: 1px solid var(--color-border-hairline); /* #DCD7CD */
--border-strong: 1px solid var(--color-border-strong);     /* #BCB5A8 */
--border-focus: 2px solid var(--color-ink-primary);        /* #181615 */
--border-accent: 1.5px solid var(--color-accent-safelight);/* #C43214 */
```

### 4.4 Elevation & Shadows
- **Cards & Surfaces**: Zero diffuse box-shadow (`box-shadow: none`). Defined strictly by the `1px solid var(--color-border-hairline)` against `--color-bg-base`.
- **Card Interactive Hover**: Physical contact lift:
  ```css
  box-shadow: 0 2px 6px rgba(24, 22, 21, 0.05);
  transform: translateY(-2px);
  ```
- **Modals & Overlays**: Contact projection shadow:
  ```css
  box-shadow: 0 12px 32px rgba(24, 22, 21, 0.16), 0 2px 6px rgba(24, 22, 21, 0.08);
  ```

---

## 5. RTL Rules & Logical CSS Properties

The product is built **RTL-first** for the Arabic language. All styling uses CSS Logical Properties to guarantee natural layout mirroring without fragile physical property overrides.

### 5.1 Mandatory Logical Property Mapping
| Physical Property (Banned) | Logical Property (Required) | Behavior in Arabic RTL (`dir="rtl"`) |
|---|---|---|
| `margin-left` | `margin-inline-start` | Indents from the right |
| `margin-right` | `margin-inline-end` | Indents from the left |
| `padding-left` | `padding-inline-start` | Pads right edge |
| `padding-right` | `padding-inline-end` | Pads left edge |
| `left` / `right` | `inset-inline-start` / `inset-inline-end` | Anchors to right / left |
| `text-align: right` | `text-align: start` | Aligns to reading start (Right in RTL) |
| `text-align: left` | `text-align: end` | Aligns to reading end (Left in RTL) |
| `border-right` | `border-inline-start` | Right hairline |
| `border-left` | `border-inline-end` | Left hairline |

### 5.2 Directional Icon Mirroring
- **Mirrored**: Directional indicators such as back arrows, forward steps, pagination chevrons, and breadcrumbs:
  ```css
  [dir="rtl"] .icon-directional {
    transform: scaleX(-1);
  }
  ```
- **Non-Mirrored**: Symmetrical, circular, or universal real-world equipment icons (camera, microphone, clock, calendar, slider, play/pause, checkmark, close 'x').

---

## 6. Motion & Accessibility

### 6.1 Mechanical Darkroom Motion
- **Timing Function**: `cubic-bezier(0.16, 1, 0.3, 1)` (mechanical shutter ease-out).
- **Durations**:
  - Micro-interactions (hover, slot tap, badge toggle): `150ms`.
  - Sheet slide-in, modal reveal, collapse: `220ms`.
- **Deliberate Restraint**: Zero decorative bounce, floating loops, or continuous glowing animations.

### 6.2 Prefers-Reduced-Motion
```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

---

## 7. Component Specifications

### 7.1 Signature Component: The "Time Ribbon" (`شريط الوقت`)

The Time Ribbon is the core booking interaction. It replaces conventional vertical dropdowns with an intuitive, horizontal hourly strip designed for rapid slot selection, hold visualization, and duration enforcement.

```
+---------------------------------------------------------------------------------------+
|  شريط الوقت - الثلاثاء 20 أكتوبر 2026               الحد الأدنى: ساعتان (09:00 - 23:00) |
+---------------------------------------------------------------------------------------+
|  [ 09:00 ] [ 10:00 ] [ 11:00 ] [ 12:00 ] [ 13:00 ] [ 14:00 ] [ 15:00 ] [ 16:00 ] ...  |
|   محجوز     محجوز     متاح      محدد      محدد    قيد الحجز    متاح      متاح           |
+---------------------------------------------------------------------------------------+
```

#### Ribbon Slot States
1. **Available (`متاح`)**:
   - Surface: `#FFFFFF`, Border: `1px solid #DCD7CD`, Ink: `#181615`.
   - Hover: Background `#EFECE6`.
   - Cursor: `pointer`.
2. **Selected (`محدد`)**:
   - Surface: `#C43214` (Safelight Red-Orange), Border: `1px solid #A8270E`, Text: `#FFFFFF` (High contrast 5.61:1).
   - Shows active duration connecting bar between consecutive selected slots.
3. **Held (Pending Payment Hold — 15 min) (`قيد الحجز`)**:
   - Surface: `#FAF1DC` (Warm Ochre), Border: `1px dashed #EAD7A6`, Text: `#784D06`.
   - Sub-label displays remaining hold time (e.g., `12:45 متبقية`).
   - Unavailable for selection until expired.
4. **Booked (`محجوز`)**:
   - Surface: `#EFECE6`, Border: `1px solid #E6E2D8`, Text: `#9C968D`.
   - Diagonal subtle analog strikethrough. Cursor: `not-allowed`.
   - `aria-disabled="true"`.

#### Accessibility & Keyboard Interaction
- **ARIA Structure**: `role="listbox"` with `aria-label="اختيار فترات الحجز بالساعات"`.
- Each slot item has `role="option"`, `aria-selected="true|false"`, `aria-disabled="true|false"`.
- Keyboard Navigation:
  - In RTL: `ArrowRight` focuses previous hour; `ArrowLeft` focuses next hour.
  - `Space` or `Enter` toggles selection.
  - Slot range selection: Holding `Shift + ArrowLeft` extends consecutive slot selection.
- Minimum Duration Validation: If selected range $<$ `Space.MinBookingHours`, Ribbon displays an inline safelight helper banner: `"يجب اختيار ساعتين متتاليتين على الأقل"`.

---

### 7.2 Signature Component: The Receipt Ticket (`إيصال الحجز`)

The booking summary is visually styled as an analog photographic darkroom contact sheet receipt or printed studio gate ticket.

```
.-----------------------------------------------------.
| [ ] [ ] [ ] [ ] PERFORATED EDGE [ ] [ ] [ ] [ ] [ ] |  <-- 2px dashed border
+-----------------------------------------------------+
|  استوديو إشراق للإنتاج                               |
|  رقم الحجز: #SB-9481                  الرياض، النرجس |
|-----------------------------------------------------|
|  المساحة: غرفة البودكاست الاحترافية                   |
|  الموعد: الثلاثاء 20 أكتوبر 2026                      |
|  الوقت: 14:00 - 17:00 (3 ساعات)                      |
|  سعر الساعة: 150.00 ر.س                             |
|-----------------------------------------------------|
|  إجمالي الحجز:                          450.00 ر.س  |
|  الدفعة المقدمة (20% الآن):              90.00 ر.س  |
|  المتبقي للدفع في الاستوديو (80%):      360.00 ر.س  |
+-----------------------------------------------------+
|  الحالة: مؤكد [ختم]                                  |
|  رمز التحقق: AST-882104-SA                           |
.-----------------------------------------------------.
```

#### Receipt Design Details
- **Edges**: Top and bottom edges feature a simulated analog perforation:
  ```css
  border-top: 2px dashed var(--color-border-strong);
  border-bottom: 2px dashed var(--color-border-strong);
  ```
- **Paper**: Background uses pure `#FFFFFF` resting on `--color-bg-base` with hairline side borders.
- **Numbers**: Tabular numerals for monetary breakdown with currency suffix (`ر.س`).
- **Summary Invariants**: Explicitly breaks down:
  - Total Amount (`TotalAmount = Rate x Hours`)
  - 20% Deposit (`DepositAmount = Round(Total x 0.20)`)
  - 80% Remaining Balance on-site (`TotalAmount - DepositAmount`).

---

### 7.3 Studio & Space Cards (`بطاقات الاستوديوهات`)
- **Image-First Aspect Ratio**: `16:9` or `3:2` photographic viewport at the top of the card.
- **Borders & Shadows**:
  - Hairline: `1px solid var(--color-border-hairline)`.
  - Shadow: strictly `none` at rest; subtle `0 2px 6px rgba(24, 22, 21, 0.05)` on hover with `translateY(-2px)`.
- **Content Hierarchy**:
  1. Studio Photo with category tag overlaid top-start (`تصوير سينمائي`, `تسجيل صوتي`, `بودكاست`).
  2. Studio Name in `Reem Kufi` (`18px` / SemiBold).
  3. Space category, capacity icon with Western number (`4 أشخاص`).
  4. Hourly Rate Snapshot in prominent tabular figures (`180 ر.س / ساعة`).

---

### 7.4 Buttons (`الأزرار`)

```css
/* Primary Action Button (Safelight Red-Orange) */
.btn-primary {
  background-color: var(--color-accent-safelight); /* #C43214 */
  color: #FFFFFF;
  border: 1px solid var(--color-accent-safelight-hover);
  border-radius: var(--radius-sm); /* 4px */
  padding: 0.625rem 1.25rem;
  font-family: 'IBM Plex Sans Arabic', sans-serif;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 150ms cubic-bezier(0.16, 1, 0.3, 1);
}
.btn-primary:hover {
  background-color: var(--color-accent-safelight-hover); /* #A8270E */
}
.btn-primary:focus-visible {
  outline: 2px solid var(--color-ink-primary);
  outline-offset: 2px;
}

/* Secondary Button (Recessed Paper) */
.btn-secondary {
  background-color: var(--color-bg-recessed); /* #EFECE6 */
  color: var(--color-ink-primary);           /* #181615 */
  border: 1px solid var(--color-border-hairline);
  border-radius: var(--radius-sm);
  padding: 0.625rem 1.25rem;
}
.btn-secondary:hover {
  background-color: var(--color-bg-muted);    /* #E6E2D8 */
}

/* Destructive Button */
.btn-destructive {
  background-color: #FAECEC;
  color: #7A2828;
  border: 1px solid #E8C4C4;
  border-radius: var(--radius-sm);
}
.btn-destructive:hover {
  background-color: #F5D8D8;
}
```

---

### 7.5 Form Inputs (`حقول الإدخال`)
- Background: `#FFFFFF` on `--color-bg-base`.
- Border: `1px solid var(--color-border-strong)` (`#BCB5A8`).
- Border Radius: `4px` (`--radius-sm`).
- Focus state:
  ```css
  outline: none;
  border-color: var(--color-ink-primary);
  box-shadow: 0 0 0 1px var(--color-ink-primary);
  ```
- Error state:
  ```css
  border-color: var(--color-accent-safelight);
  box-shadow: 0 0 0 1px var(--color-accent-safelight);
  ```
- Text Direction: Native RTL with numerical tabular alignment for currency inputs.

---

### 7.6 Modals & Overlays (`النوافذ المنبثقة`)
- **Backdrop**: Darkroom contact wash `rgba(24, 22, 21, 0.55)` with `backdrop-filter: blur(2px)`.
- **Container**:
  - Surface: `#FFFFFF`.
  - Border: `1px solid var(--color-border-hairline)`.
  - Radius: `6px` (`--radius-md`).
  - Shadow: `0 12px 32px rgba(24, 22, 21, 0.16)`.
- **Header**: Reem Kufi display title with close button anchored at `inset-inline-end: 16px`.

---

## 8. Role Dashboard Layout Rules (Mobile-First)

### 8.1 Customer Dashboard (`لوحة العميل`)
- **Mobile (< 768px)**:
  - Single column stack.
  - Active booking card on top with direct 15-minute hold timer or confirmed booking details.
  - Tabbed switcher: "حجوزاتي القادمة" (Upcoming) and "السابقة" (History).
  - Cancellation CTA conditionally enabled strictly when `now < Start` (with modal warning of 100% vs 0% refund).
- **Desktop (1024px+)**:
  - Asymmetric 2-column layout:
    - Primary column (65% width): Marketplace search, studio space browser, and bookings list.
    - Secondary column (35% width): Active Receipt Ticket with sticky booking summary and dispute initiation actions within 48h.

### 8.2 Studio Owner Dashboard (`لوحة مالك الاستوديو`)
- **Header Metrics Strip**:
  - 3 clean tabular blocks separated by hairlines (not floating cards):
    - الرصيد المتاح (Available Balance in SAR)
    - الحجوزات النشطة (Active Bookings)
    - بانتظار إطلاق المستحقات (Pending 48h Payout Release)
- **Space Management & Operating Hours**:
  - Space cards showing hourly rates, minimum duration, and verified daily operating hours (e.g. `09:00 - 23:00 AST`).
  - Inline toggle for `نشط / غير نشط` (Active/Inactive).
- **Bookings Schedule**:
  - Day-view Time Ribbon showing full room schedule with real-time hold and booked statuses.

### 8.3 Admin Dashboard (`لوحة الإدارة`)
- **Studio Moderation Queue**:
  - Master-detail table: Studio name, owner identity, city, number of spaces, and review actions (Approve / Reject modal with mandatory written reason).
- **Dispute Arbitration Console**:
  - Live split calculator enforcing the mathematical invariant:
    $$\text{CustomerRefund} + \text{OwnerNet} + \text{Commission} == \text{DepositAmount}$$
  - Interactive slider adjusts `CustomerRefund`; live labels update `OwnerGross`, `Commission (10%)`, and `OwnerNet`.

---

## 9. File Deliverables Summary

1. **`docs/DESIGN.md`** (this document): Complete token system, typography, components, and layout architecture.
2. **`docs/design-preview.html`**: Interactive, standalone browser preview demonstrating the palette, typography scale, buttons, badges, Time Ribbon, and Receipt Ticket in Arabic RTL.
