import { DocumentList } from '../../features/documents/model/document';
import { DocumentListQuery } from '../../features/documents/services/document-service';

export type DocumentListMode = 'all' | 'pending';

export const defaultDocumentListQuery: DocumentListQuery = {
  page: 1,
  pageSize: 10,
  sortBy: 'createdAt',
  sortDirection: 'desc',
};

export interface DocumentListState {
  items: DocumentList[];
  query: DocumentListQuery;
  mode: DocumentListMode;
  isLoading: boolean;
  errorMessage: string;
  isActionPending: boolean;
  actionError: string | null;
  actionSucceeded: boolean;
  totalItems: number;
}

export const initialDocumentListState: DocumentListState = {
  items: [],
  query: defaultDocumentListQuery,
  mode: 'all',
  isLoading: false,
  errorMessage: '',
  isActionPending: false,
  actionError: null,
  actionSucceeded: false,
  totalItems: 0,
};
