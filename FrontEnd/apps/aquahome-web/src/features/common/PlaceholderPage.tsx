import { useLocation, useNavigate } from 'react-router-dom';
import { Construction, Home, ArrowLeft } from 'lucide-react';

export default function PlaceholderPage({ is404 = false }: { is404?: boolean }) {
  const location = useLocation();
  const navigate = useNavigate();
  const path = location.pathname;

  let title = 'Coming Soon';
  let description = 'We are working hard to bring this feature to life. Stay tuned!';

  if (is404) {
    title = '404 - Page Not Found';
    description = `The page you are looking for (${path}) doesn't exist or has been moved.`;
  } else {
    // Customize based on path
    if (path.includes('/tanks')) title = 'My Tanks (AquaHome)';
    if (path.includes('/parameters')) title = 'Water Parameters & Logs';
    if (path.includes('/tasks')) title = 'Tasks & Reminders';
    if (path.includes('/favorites')) title = 'My Favorites';
    if (path.includes('/history')) title = 'Search History';
    if (path.includes('/my-fish')) title = 'My Fish Collection';
    if (path.includes('/dashboard')) title = 'Dashboard Overview';
    if (path.includes('/ai-chat')) title = 'AI Chat Assistant';
    if (path.includes('/image-search')) title = 'Fish Identification via Image';
  }

  return (
    <div className="flex flex-col items-center justify-center w-full h-full min-h-[400px] text-center p-8">
      <div className="bg-[#202226] border border-slate-800/80 rounded-3xl p-10 max-w-md w-full shadow-2xl flex flex-col items-center relative overflow-hidden">
        {/* Glow effect */}
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-32 h-32 bg-primary/20 blur-3xl rounded-full" />
        
        <div className="relative z-10 bg-[#141518] p-4 rounded-2xl border border-slate-700/50 mb-6 shadow-inner">
          <Construction className="w-12 h-12 text-primary stroke-[1.5]" />
        </div>
        
        <h2 className="text-2xl font-bold text-white mb-2 relative z-10">{title}</h2>
        <p className="text-slate-400 text-sm mb-8 relative z-10 leading-relaxed">
          {description}
        </p>

        <div className="flex gap-3 w-full relative z-10">
          <button 
            onClick={() => navigate(-1)}
            className="flex-1 flex items-center justify-center gap-2 bg-[#141518] hover:bg-slate-800 border border-slate-700/50 text-slate-300 py-2.5 px-4 rounded-xl font-medium transition-colors"
          >
            <ArrowLeft className="w-4 h-4" /> Back
          </button>
          <button 
            onClick={() => navigate('/fish')}
            className="flex-1 flex items-center justify-center gap-2 bg-primary hover:bg-blue-600 text-white py-2.5 px-4 rounded-xl font-medium transition-colors shadow-lg shadow-primary/20"
          >
            <Home className="w-4 h-4" /> Home
          </button>
        </div>
      </div>
    </div>
  );
}
