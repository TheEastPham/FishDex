import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { useAuthStore } from '@fishlover/shared';
// Đường đăng nhập + khung app tải ngay (nhỏ, cần cho lần vẽ đầu tiên).
import LoginPage from '@/features/auth/LoginPage';
import CallbackPage from '@/features/auth/CallbackPage';
import RegisterPage from '@/features/auth/RegisterPage';
import ForgotPasswordPage from '@/features/auth/ForgotPasswordPage';
import ResetPasswordPage from '@/features/auth/ResetPasswordPage';
import AuthGuard from '@/components/AuthGuard';
import RoleGuard from '@/components/RoleGuard';
import PageLoader from '@/components/common/PageLoader';
import AppShell from '@/layouts/AppShell';

// Mọi page còn lại tải lazy theo route: mỗi page một chunk riêng, bundle đầu không phải mang
// Leaflet, trình soạn bài viết, trang admin... Suspense cho page trong AppShell nằm quanh <Outlet />.
const FishSearchPage = lazy(() => import('@/features/fish-search/FishSearchPage'));
const MarketPage = lazy(() => import('@/features/market/MarketPage'));
const AdminMarketPage = lazy(() => import('@/features/market/AdminMarketPage'));
const FishProfilePage = lazy(() => import('@/features/fish-profile/FishProfilePage'));
const PlaceholderPage = lazy(() => import('@/features/common/PlaceholderPage'));
const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'));
const TanksPage = lazy(() => import('@/features/tanks/TanksPage'));
const FavoritesPage = lazy(() => import('@/features/favorites/FavoritesPage'));
const ProfilePage = lazy(() => import('@/features/profile/ProfilePage'));
const HistoryPage = lazy(() => import('@/features/history/HistoryPage'));
const ReleasePage = lazy(() => import('@/features/articles/ReleasePage'));
const ArticlesListPage = lazy(() => import('@/features/articles/ArticlesListPage'));
const ArticleDetailPage = lazy(() => import('@/features/articles/ArticleDetailPage'));
const TasksPage = lazy(() => import('@/features/tasks/TasksPage'));
const PublicTanksPage = lazy(() => import('@/features/public-tanks/PublicTanksPage'));
const PublicTankDetailPage = lazy(() => import('@/features/public-tanks/PublicTankDetailPage'));
const ContestsPage = lazy(() => import('@/features/contests/ContestsPage'));
const AdminContestsPage = lazy(() => import('@/features/admin-contests/AdminContestsPage'));
const MyPublishedTanksPage = lazy(() => import('@/features/my-published-tanks/MyPublishedTanksPage'));
const MyContributionsPage = lazy(() => import('@/features/community/MyContributionsPage'));
const SubmitSpeciesPage = lazy(() => import('@/features/community/SubmitSpeciesPage'));
const AdminCommunityPage = lazy(() => import('@/features/admin-community/AdminCommunityPage'));
const AdminArticlesPage = lazy(() => import('@/features/admin-articles/AdminArticlesPage'));
const LegalPage = lazy(() => import('@/features/legal/LegalPage'));

// Redirect "/" theo trạng thái đăng nhập.
// Khách chưa đăng nhập vào /market chứ không phải /fish: trang tra cứu không có gì để xem cho
// tới khi gõ tên loài, mà người mới nuôi cá thì chưa biết tên nào để gõ. /market vào là thấy
// nội dung ngay — ấn tượng đầu tốt hơn hẳn một ô tìm kiếm rỗng.
function RootRedirect() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  return <Navigate to={isAuthenticated ? '/dashboard' : '/market'} replace />;
}

export const router = createBrowserRouter([
  { path: '/login',           element: <LoginPage /> },
  { path: '/register',        element: <RegisterPage /> },
  { path: '/forgot-password', element: <ForgotPasswordPage /> },
  { path: '/reset-password',  element: <ResetPasswordPage /> },
  { path: '/callback',        element: <CallbackPage /> },
  // Ngoài AppShell và ngoài AuthGuard — Google phải đọc được hai URL này để duyệt
  // OAuth consent screen, nên chúng mở cho cả khách chưa đăng nhập.
  { path: '/privacy',         element: <Suspense fallback={<PageLoader />}><LegalPage doc="privacy" /></Suspense> },
  { path: '/terms',           element: <Suspense fallback={<PageLoader />}><LegalPage doc="terms" /></Suspense> },
  {
    element: <AppShell />,
    children: [
      { index: true, element: <RootRedirect /> },

      // ── Public routes ──────────────────────────────────────
      { path: '/market',              element: <MarketPage /> },
      { path: '/market/:cc',          element: <MarketPage /> },
      { path: '/fish',                element: <FishSearchPage /> },
      { path: '/fish/:specCode',      element: <FishProfilePage /> },
      { path: '/articles',            element: <ArticlesListPage /> },
      // Route tĩnh phải đứng trước /articles/:slug, không thì "release" bị hiểu là slug bài viết
      { path: '/articles/release',    element: <ReleasePage /> },
      { path: '/articles/:slug',      element: <ArticleDetailPage /> },
      { path: '/public/tanks',        element: <PublicTanksPage /> },
      { path: '/public/tanks/:slug',  element: <PublicTankDetailPage /> },
      { path: '/contests',            element: <ContestsPage /> },

      // ── Auth-required routes ───────────────────────────────
      {
        element: <AuthGuard />,
        children: [
          { path: '/dashboard',    element: <DashboardPage /> },
          { path: '/tanks',        element: <TanksPage /> },
          { path: '/my-published-tanks', element: <MyPublishedTanksPage /> },
          { path: '/my-contributions', element: <MyContributionsPage /> },
          { path: '/submit-species', element: <SubmitSpeciesPage /> },
          { path: '/parameters',   element: <PlaceholderPage /> },
          { path: '/tasks',        element: <TasksPage /> },
          { path: '/favorites',    element: <FavoritesPage /> },
          { path: '/profile',      element: <ProfilePage /> },
          { path: '/history',      element: <HistoryPage /> },
          { path: '/my-fish',      element: <PlaceholderPage /> },
          { path: '/ai-chat',      element: <PlaceholderPage /> },
          { path: '/image-search', element: <PlaceholderPage /> },
        ],
      },

      // ── Role-protected routes ──────────────────────────────
      {
        element: <RoleGuard roles={['SystemAdmin', 'ContentAdmin']} />,
        children: [
          { path: '/admin/articles', element: <AdminArticlesPage /> },
          { path: '/admin/media',    element: <PlaceholderPage /> },
          { path: '/admin/community', element: <AdminCommunityPage /> },
          { path: '/admin/market',    element: <AdminMarketPage /> },
        ],
      },
      {
        element: <RoleGuard roles={['SystemAdmin']} />,
        children: [
          { path: '/admin/contests', element: <AdminContestsPage /> },
        ],
      },

      { path: '*', element: <PlaceholderPage is404 /> },
    ],
  },
]);
