import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

interface SidebarState {
  isExpanded: boolean;
  isMobileOpen: boolean;
  isHovered: boolean;
}

const initialSidebarState: SidebarState = {
  isExpanded: true,
  isMobileOpen: false,
  isHovered: false,
};

export const SidebarStore = signalStore(
  { providedIn: 'root' },
  withState(initialSidebarState),
  withMethods((store) => ({
    setExpanded(isExpanded: boolean): void {
      patchState(store, { isExpanded });
    },
    toggleExpanded(): void {
      patchState(store, (state) => ({ isExpanded: !state.isExpanded }));
    },
    setMobileOpen(isMobileOpen: boolean): void {
      patchState(store, { isMobileOpen });
    },
    toggleMobileOpen(): void {
      patchState(store, (state) => ({ isMobileOpen: !state.isMobileOpen }));
    },
    setHovered(isHovered: boolean): void {
      patchState(store, { isHovered });
    },
  })),
);

export type SidebarStore = InstanceType<typeof SidebarStore>;
