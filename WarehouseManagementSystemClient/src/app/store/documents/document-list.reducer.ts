import { createReducer, on } from '@ngrx/store';
import { DocumentListActions } from './document-list.actions';
import { initialDocumentListState } from './document-list.state';

export const documentListReducer = createReducer(
  initialDocumentListState,
  on(DocumentListActions.load, (state, { mode, query }) => {
    const modeChanged = state.mode !== mode;
    return {
      ...state,
      mode,
      query,
      items: modeChanged ? [] : state.items,
      totalItems: modeChanged ? 0 : state.totalItems,
      isLoading: true,
      errorMessage: '',
    };
  }),
  on(DocumentListActions.loadSuccess, (state, { result }) => ({
    ...state,
    items: result.items,
    totalItems: result.totalItems,
    query: {
      ...state.query,
      page: result.page,
      pageSize: result.pageSize,
    },
    isLoading: false,
  })),
  on(DocumentListActions.loadFailure, (state, { error }) => ({
    ...state,
    items: [],
    totalItems: 0,
    isLoading: false,
    errorMessage: error,
  })),
  on(DocumentListActions.confirmSuccess, DocumentListActions.cancelSuccess, (state) => ({
    ...state,
  })),
  on(DocumentListActions.confirmFailure, (state, { error }) => ({
    ...state,
    isActionPending: false,
    errorMessage: 'Document could not be confirmed. Please try again.',
    actionError: error,
  })),
  on(DocumentListActions.cancelFailure, (state, { error }) => ({
    ...state,
    isActionPending: false,
    errorMessage: 'Document could not be cancelled. Please try again.',
    actionError: error,
  })),
  on(DocumentListActions.confirm, DocumentListActions.cancel, (state) => ({
    ...state,
    isActionPending: true,
    actionError: null,
    actionSucceeded: false,
  })),
  on(DocumentListActions.confirmSuccess, DocumentListActions.cancelSuccess, (state) => ({
    ...state,
    isActionPending: false,
    actionError: null,
    actionSucceeded: true,
  })),
);
