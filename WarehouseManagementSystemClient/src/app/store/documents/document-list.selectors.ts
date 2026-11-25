import { createFeatureSelector, createSelector } from '@ngrx/store';
import { DocumentListState } from './document-list.state';

export const DOCUMENT_LIST_FEATURE_KEY = 'documentList';

export const selectDocumentListState =
  createFeatureSelector<DocumentListState>(DOCUMENT_LIST_FEATURE_KEY);

export const selectDocumentItems = createSelector(
  selectDocumentListState,
  (state) => state.items,
);

export const selectDocumentQuery = createSelector(
  selectDocumentListState,
  (state) => state.query,
);

export const selectDocumentListLoading = createSelector(
  selectDocumentListState,
  (state) => state.isLoading,
);

export const selectDocumentListError = createSelector(
  selectDocumentListState,
  (state) => state.errorMessage,
);

export const selectDocumentTotalItems = createSelector(
  selectDocumentListState,
  (state) => state.totalItems,
);

export const selectDocumentActionPending = createSelector(
  selectDocumentListState,
  (state) => state.isActionPending,
);

export const selectDocumentActionError = createSelector(
  selectDocumentListState,
  (state) => state.actionError,
);

export const selectDocumentActionSucceeded = createSelector(
  selectDocumentListState,
  (state) => state.actionSucceeded,
);
