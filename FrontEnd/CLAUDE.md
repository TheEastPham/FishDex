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
- `apps/aquahome-web` — React 19 + Vite + TypeScript, Tailwind CSS, icon `lucide-react`. Không dùng thư viện component (shadcn đã gỡ 11/2026); gộp class bằng `cn()` của `@fishlover/shared` (clsx + tailwind-merge).

## i18n

Mọi string hiển thị cho user phải đi qua `useTranslation()` — không hardcode text tiếng Việt hay tiếng Anh trực tiếp vào JSX.

Thêm key mới vào **cả 4 file** `vi.ts`, `en.ts`, `de.ts`, `zh.ts` cùng lúc (menu chọn ngôn ngữ có cả 4). Kiểm tra đủ key: `npm run i18n:check -w @fishlover/shared` (thiếu/thừa → exit 1).

## Test

`npm test` ở `FrontEnd/` (Vitest, cấu hình `vitest.config.ts`). File `*.test.ts(x)` đặt cạnh code; mặc định chạy trong Node, test cần DOM thì thêm dòng đầu `// @vitest-environment jsdom` và render bằng `@testing-library/react`. Logic trong `packages/shared` (API client, auth, cache) phải có test — app mobile sau này dùng lại phần này.

## Commits

KHÔNG gộp code BE (`BackEndProject/`, `Pipeline/`) và FE (`FrontEnd/`) trong cùng 1 commit khi implement feature — tách commit riêng cho mỗi bên. **Ngoại lệ**: fix bug thì được gộp BE+FE trong 1 commit.
