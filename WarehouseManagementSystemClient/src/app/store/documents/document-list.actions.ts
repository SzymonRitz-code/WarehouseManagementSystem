import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { PagedResult, DocumentListQuery } from '../../features/documents/services/document-service';
import { DocumentList } from '../../features/documents/model/document';
import { DocumentListMode } from './document-list.state';

export const DocumentListActions = createActionGroup({
  source: 'Document List',
  events: {
    Load: props<{ mode: DocumentListMode; query: DocumentListQuery }>(),
    LoadSuccess: props<{ result: PagedResult<DocumentList> }>(),
    LoadFailure: props<{ error: string }>(),
    Confirm: props<{ document: Pick<DocumentList, 'id'> }>(),
    ConfirmSuccess: emptyProps(),
    ConfirmFailure: props<{ error: string }>(),
    Cancel: props<{ document: Pick<DocumentList, 'id'> }>(),
    CancelSuccess: emptyProps(),
    CancelFailure: props<{ error: string }>(),
  },
});
