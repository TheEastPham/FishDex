# CLAUDE.md — FrontEnd

This file provides guidance to Claude Code when working with FrontEnd code in this repository.

## Mobile-First Rule — iPhone 12+ (390px)

**Mọi component/page mới đều phải hoạt động tốt trên iPhone 12 (390px CSS viewport, Safari).**

Người dùng The FishLover chủ yếu truy cập bằng điện thoại — đứng trước bể cá, check thông số, tra cứu loài. Desktop là secondary.

Khi thêm mới hoặc sửa UI, luôn tự hỏi:
- Layout này trên 390px trông thế nào? 1 cột hay vỡ?
- Tap target có đủ 44×44px chưa? (Apple HIG)
- Input font-size có ≥ 16px không? (tránh Safari auto-zoom)
- Có scroll ngang không mong muốn không?
- Map (Leaflet) có chiếm toàn màn hình không thoát được không?

**Breakpoint chuẩn dùng trong project:**
- Mobile: `< 768px` (default / no prefix trong Tailwind — mobile-first)
- Tablet: `md:` 768px+
- Desktop: `lg:` 1024px+

**Navigation trên mobile (< 768px):** sidebar ẩn, dùng bottom nav bar hoặc hamburger drawer.

## Package Structure

- `packages/shared` — types, API clients, hooks, i18n, store. Dùng chung cho mọi app.
- `apps/aquahome-web` — React 19 + Vite + TypeScript, Tailwind CSS, shadcn/ui.

## i18n

Mọi string hiển thị cho user phải đi qua `useTranslation()` — không hardcode text tiếng Việt hay tiếng Anh trực tiếp vào JSX.

Thêm key mới vào cả `vi.ts` và `en.ts` cùng lúc.

## Commits

CLAUDE.md `BackEndProject/` có rule: chỉ commit `BackEndProject/` và `Pipeline/`. FE tự quản lý commit riêng — không bao giờ stage FrontEnd/ khi đang làm task BE.
