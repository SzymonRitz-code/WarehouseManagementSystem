import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap, withLatestFrom } from 'rxjs';
import { Store } from '@ngrx/store';
import { DocumentService } from '../../features/documents/services/document-service';
import { DocumentListActions } from './document-list.actions';
import { selectDocumentListState } from './document-list.selectors';

@Injectable()
export class DocumentListEffects {
  private readonly actions$ = inject(Actions);
  private readonly documentService = inject(DocumentService);
  private readonly store = inject(Store);

  loadDocuments$ = createEffect(() =>
    this.actions$.pipe(
      ofType(DocumentListActions.load),
      switchMap(({ mode, query }) => {
        const request$ = mode === 'pending'
          ? this.documentService.getPendingDocuments(query)
          : this.documentService.getDocuments(query);
        return request$.pipe(
          map((result) => DocumentListActions.loadSuccess({ result })),
          catchError(() => of(DocumentListActions.loadFailure({
            error: mode === 'pending'
              ? 'Pending documents could not be loaded. Please try again.'
              : 'Documents could not be loaded. Please try again.',
          }))),
        );
      }),
    ),
  );

  confirmDocument$ = createEffect(() =>
    this.actions$.pipe(
      ofType(DocumentListActions.confirm),
      switchMap(({ document }) =>
        this.documentService.confirmDocument(document).pipe(
          map(() => DocumentListActions.confirmSuccess()),
          catchError((error: unknown) => of(DocumentListActions.confirmFailure({
            error: this.resolveActionError(error, 'Document could not be confirmed. Please try again.'),
          }))),
        ),
      ),
    ),
  );

  cancelDocument$ = createEffect(() =>
    this.actions$.pipe(
      ofType(DocumentListActions.cancel),
      switchMap(({ document }) =>
        this.documentService.cancelDocument(document).pipe(
          map(() => DocumentListActions.cancelSuccess()),
          catchError((error: unknown) => of(DocumentListActions.cancelFailure({
            error: this.resolveActionError(error, 'Document could not be cancelled. Please try again.'),
          }))),
        ),
      ),
    ),
  );

  refreshAfterDocumentAction$ = createEffect(() =>
    this.actions$.pipe(
      ofType(DocumentListActions.confirmSuccess, DocumentListActions.cancelSuccess),
      withLatestFrom(this.store.select(selectDocumentListState)),
      map(([, { mode, query }]) => DocumentListActions.load({ mode, query })),
    ),
  );

  private resolveActionError(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null) {
      const httpError = error as {
        error?: { detail?: string; title?: string };
        message?: string;
      };
      return httpError.error?.detail
        ?? httpError.error?.title
        ?? httpError.message
        ?? fallback;
    }
    return fallback;
  }
}
