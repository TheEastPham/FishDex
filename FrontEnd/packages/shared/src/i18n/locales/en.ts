const en = {
  nav: {
    dashboard: 'Dashboard',
    fishSearch: 'Fish Search',
    aiChat: 'AI Chat',
    imageSearch: 'Image Search',
    logout: 'Logout',
  },
  login: {
    subtitle: 'Manage your aquarium',
    button: 'Login',
  },
  fish: {
    title: 'Fish Search',
    subtitle: 'Search in ~8,883 aquarium species from TheFishLover',
    placeholder: 'Scientific name or common name...',
    allFamilies: 'All Families',
    results: 'results for',
    error: 'Unable to load results. Check FishDex Service.',
    emptyResult: 'No species found for',
    emptyState: 'Enter a species name to start searching',
    genus: 'Genus',
    detail: 'Detail',
    viewProfile: 'View Profile',
    viewProfileDetails: 'View Profile Details',
    viewFamily: 'View all in {{family}} family',
    unknownFamily: 'Unknown',
    share: 'Share',
    addToFavorites: 'Add to Favorites',
    addToAquarium: '+ Add to Aquarium',
    addToAquariumDisabledTip: 'Requires AquaHome Service (Story 5.2)',
  },
  pagination: {
    prev: '← Previous',
    next: 'Next →',
    page: 'Page',
    of: '/',
  },
} as const;

export default en;
