import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { UserService } from '../../../services/user-service';
import { User } from '../../model/user';
import { UserListComponent } from './user-list.component';

describe('UserListComponent', () => {
  let component: UserListComponent;
  let fixture: ComponentFixture<UserListComponent>;
  let userService: {
    getUsers: ReturnType<typeof vi.fn>;
  };
  let router: {
    navigate: ReturnType<typeof vi.fn>;
    navigateByUrl: ReturnType<typeof vi.fn>;
  };

  const userRow: User = {
    id: '1',
    username: 'operator',
    firstName: 'Warehouse',
    lastName: 'Operator',
    email: 'operator@example.com',
    role: 'Operator',
    status: true,
    createdAt: new Date('2026-06-22T08:00:00Z')
  };

  beforeEach(async () => {
    userService = {
      getUsers: vi.fn().mockReturnValue(of([userRow]))
    };
    router = {
      navigate: vi.fn(),
      navigateByUrl: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [UserListComponent],
      providers: [
        { provide: UserService, useValue: userService },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;

    const angularRouter = TestBed.inject(Router);
    vi.spyOn(angularRouter, 'navigate').mockImplementation((commands: readonly any[]) => {
      (router.navigate as any)(commands);
      return Promise.resolve(true);
    });
    vi.spyOn(angularRouter, 'navigateByUrl').mockImplementation((url: any) => {
      (router.navigateByUrl as any)(url);
      return Promise.resolve(true);
    });

    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('loads users on init and clears loading state', async () => {
    const users = await firstUsersEmission();

    expect(userService.getUsers).toHaveBeenCalledTimes(1);
    expect(users).toEqual([userRow]);
    expect(component.isLoading).toBe(false);
    expect(component.errorMessage).toBe('');
  });

  it('keeps loading true while the current users request is pending', () => {
    const pendingRequest$ = new Subject<User[]>();
    userService.getUsers.mockReturnValue(pendingRequest$);

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.users$.subscribe();

    expect(component.isLoading).toBe(true);

    pendingRequest$.next([]);
    pendingRequest$.complete();

    expect(component.isLoading).toBe(false);
  });

  it('exposes an error state and empty rows when API fails', async () => {
    await firstUsersEmission();
    userService.getUsers.mockReturnValue(throwError(() => new Error('timeout')));

    component.retry();
    const users = await firstUsersEmission();

    expect(users).toEqual([]);
    expect(component.errorMessage).toBe('Users could not be loaded. Please try again.');
    expect(component.isLoading).toBe(false);
  });

  it('reloads users when retry is triggered', async () => {
    await firstUsersEmission();
    const refreshedUser = { ...userRow, id: '2', username: 'manager', role: 'Manager' };
    userService.getUsers.mockReturnValue(of([refreshedUser]));

    component.retry();
    const users = await firstUsersEmission();

    expect(userService.getUsers).toHaveBeenCalledTimes(2);
    expect(users).toEqual([refreshedUser]);
  });

  it('navigates to create, detail and edit routes', () => {
    component.goToForm();
    component.onUserAction({ row: userRow, action: 'details' });
    component.onUserAction({ row: userRow, action: 'edit' });

    expect(router.navigate).toHaveBeenCalledWith(['/users/form']);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/users/detail/1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/users/form/1');
  });

  function firstUsersEmission(): Promise<User[]> {
    return new Promise(resolve => {
      component.users$.subscribe(users => resolve(users));
    });
  }
});
