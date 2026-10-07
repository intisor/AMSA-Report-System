# Design System Specification — AMSA Nigeria Reporting System

This document establishes the official visual design system, UI/UX guidelines, and component specifications for the **AMSA Nigeria Reporting System**. It serves as a unified reference for Blazor components, standard CSS stylesheets, and Google Stitch AI modeling.

---

## 1. Visual Brand Language & Philosophy

The AMSA Nigeria Reporting System balances modern software ergonomics with the rich, prestigious heritage of the Ahmadi Muslim Students' Association. 
* **The "Amanah" Trust Principle**: Reporting is an act of voluntary, spiritual stewardship. The design must feel clean, highly respectful, reassuring, and user-centric.
* **Warm & Prestigious Aesthetics**: Avoid cold, stark corporate grays. The interface uses a carefully harmonized theme centered around deep forest greens, rich emeralds, warm heritage gold accents, and a comfortable warm linen cream background that reduces optical fatigue.

---

## 2. Design Tokens

### 2.1 Color Palette
The color tokens are specified as HSL values for seamless blending, overlay creation, and state shifts.

```css
:root {
  /* Brand Core Colors */
  --primary-deep: hsl(141, 40%, 19%);      /* #1E462B - Deep Forest Green (Dignity, Stability) */
  --primary-brand: hsl(134, 39%, 30%);     /* #2F6B3F - Emerald Green (Growth, Vitality) */
  --primary-light: hsl(144, 30%, 95%);     /* #EFF7F2 - Mint Tint (Soft hover fills, light badges) */
  
  /* Heritage Accents */
  --accent-gold: hsl(35, 38%, 64%);       /* #C5A880 - Warm Gold (Prestige, Excellence) */
  --accent-gold-glow: hsla(35, 38%, 64%, 0.15);
  
  /* Neutral Canvas & Surfaces */
  --bg-cream: hsl(38, 33%, 96%);           /* #FAF7F2 - Warm Linen (Primary body canvas) */
  --surface-white: hsl(0, 0%, 100%);       /* #FFFFFF - Pure White (Elevated card panels) */
  --border-subtle: hsl(38, 20%, 86%);      /* #E6E0D5 - Warm Linen Grey (Clean separators) */
  
  /* User Feedback / Semantic Colors */
  --color-success: hsl(154, 67%, 32%);     /* #1B8A5A - Soft Emerald (Compliance satisfied) */
  --color-success-bg: hsl(154, 67%, 95%);
  --color-warning: hsl(35, 92%, 44%);      /* #D97706 - Warm Amber (Approaching deadlines) */
  --color-warning-bg: hsl(35, 92%, 96%);
  --color-danger: hsl(347, 77%, 50%);      /* #E11D48 - Rose Red (Overdue, rejected states) */
  --color-danger-bg: hsl(347, 77%, 96%);
}
```

### 2.2 Typography
To ensure a prestigious, editorial feel combined with high analytical readability:
* **Primary Headings**: `Outfit` or `Playfair Display`, Semibold/Bold. Used for landing page taglines, dashboard greetings, and major category headings.
* **Interface & Body Text**: `Inter` or `System UI defaults`, Regular/Medium/Semibold. Used for form inputs, table data, body paragraphs, and labels.

```css
body {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
  font-size: 15px;
  line-height: 1.6;
  color: hsl(141, 40%, 12%); /* Dark forest tint for body text */
}

h1, h2, h3, h4, h5, h6 {
  font-family: 'Outfit', 'Playfair Display', serif;
  font-weight: 700;
  color: var(--primary-deep);
}
```

### 2.3 Depth & Elevation (Shadows & Radii)
All cards and interactive modules utilize premium micro-depth styling.

```css
:root {
  --radius-sm: 8px;   /* Small badges, inner controls, alerts */
  --radius-md: 16px;  /* Standard input fields, component modules, tables */
  --radius-lg: 24px;  /* Primary container boards, landing cards, modals */
  
  /* Soft Forest Shadows */
  --shadow-sm: 0 2px 8px hsla(141, 40%, 19%, 0.04);
  --shadow-md: 0 8px 24px hsla(141, 40%, 19%, 0.06);
  --shadow-lg: 0 16px 40px hsla(141, 40%, 19%, 0.1);
}
```

---

## 3. Layout & Structure Architecture

The app conforms to a responsive grid architecture, ensuring structural consistency across mobile phones (essential for local officers on-the-go) and desktop screens (for national executives).

### 3.1 App Shell (Main Layout)
* **Navbar**: A premium, sticky top navigation anchored with a brand gradient background (`linear-gradient(135deg, var(--primary-deep) 0%, var(--primary-brand) 100%)`). Feature a clean gold-bordered brand label (`📋 AMSA Reporting`) and a real-time connectivity status badge.
* **Canvas Margins**: Standard body container pad is `30px` on desktop and `12px` on mobile. Central workspace layers use the `--bg-cream` variable to isolate the dashboard boards.
* **Footer**: A minimal, light background container featuring soft text and a clean `1px solid var(--border-subtle)` separator line.

---

## 4. UI Components Specification

### 4.1 Buttons (CTAs)
Buttons should feel tactile, active, and robust.

* **Primary CTA (`.btn-primary`)**:
  - Background: `linear-gradient(135deg, var(--primary-brand) 0%, var(--primary-deep) 100%)`.
  - Border: None.
  - Text Color: `#FFFFFF` (Semibold).
  - Hover State: Scale up slightly (`transform: translateY(-2px)`), increase shadow (`box-shadow: var(--shadow-md)`), and glow.
* **Secondary CTA (`.btn-secondary`)**:
  - Background: Outline style with border `2px solid var(--primary-brand)`.
  - Text Color: `var(--primary-brand)`.
  - Hover State: Invert colors to solid green with white text.
* **Destructive CTA (`.btn-danger`)**:
  - Background: Solid `var(--color-danger)` or outlines. Used exclusively for rejecting sections or resetting reports.

### 4.2 Form Fields & Text Areas
Designed for stress-free, focused data entry.
* **Inputs & Selects**:
  - Background: `var(--surface-white)`.
  - Border: `1px solid var(--border-subtle)` (Radius: `var(--radius-sm)`).
  - Focus State: Smooth outline transition to `2px solid var(--accent-gold)` accompanied by a gold glow shadow ring (`box-shadow: 0 0 0 4px var(--accent-gold-glow)`).
  - Labeling: Form labels are stacked vertically above the field in regular weight with a small line height to keep alignment crisp.
* **Draft Auto-Save indicator**:
  - Displays a tiny, spinning sync indicator next to a "Draft saved to cloud" text block in the card footer to reassure student officers that data is secure.

### 4.3 Semantic Indicators & Status Badges
Status indicators map directly to the multi-level reporting state machine.

| Report/Section State | Badge Background | Badge Text Color | Meaning / Action Required |
| :--- | :--- | :--- | :--- |
| **Draft** | `var(--color-warning-bg)` | `var(--color-warning)` | Section is editable, not yet finalized. |
| **Submitted (Section)** | `var(--color-success-bg)` | `var(--color-success)` | Section locked for officer, pending President review. |
| **Submitted to President**| `var(--primary-light)` | `var(--primary-brand)` | Entire report awaiting Unit President sign-off. |
| **Rejected** | `var(--color-danger-bg)` | `var(--color-danger)` | Returned to Draft state with correction comments. |
| **Approved by State** | `var(--color-success-bg)` | `var(--color-success)` | Forwarded to National queue. |
| **Acknowledged** | `linear-gradient(90deg, #1b8a5a, #2f6b3f)`| `#FFFFFF` (Glow) | Completed cycle record, archived into historical database. |

### 4.4 Analytical Widgets & Performance Meters
* **Metric Cards**: Flat white boxes with a solid `5px` colored border-left indicating status (e.g., green for healthy compliance, red for escalations).
* **Live Compliance Rings**: Used in Taleem (educational attendance >= 75%) and Finance (expectations collected >= 100%).
  - Dynamically recalculate color based on percentage values: Red (< 60%), Amber (60%-74%), and Emerald Green (>= 75%).
  - Smooth 0.5s stroke-dasharray animation transition upon numerical updates.

---

## 5. Accessibility & Responsive Behaviors

* **Contrast Integrity**: All text must maintain a minimum contrast ratio of 4.5:1 against its background. Gold elements must only be placed against deep greens or white surfaces with a thick dark font weight.
* **Mobile Comfort**: Under `640px` resolution, the two-column dashboards stack vertically. Inputs must expand to a minimum touch-target size of `44px` in height for easy finger taps.
* **Transitions**: Use `all 0.3s cubic-bezier(0.4, 0, 0.2, 1)` for page routing animations, modal triggers, and form expansions to provide a smooth, organic feel.
