import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { UserService } from '../../../services/user-service';
import { User } from '../../model/user';
import { UserDetailComponent } from './user-detail.component';

describe('UserDetailComponent', () => {
  let component: UserDetailComponent;
  let fixture: ComponentFixture<UserDetailComponent>;
  let userService: {
    getUsers: ReturnType<typeof vi.fn>;
    getUser: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  it('loads user from local cache without calling users endpoint', async () => {
    const cachedUser = userFixture();
    await setup('1', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(cachedUser)
      }
    });

    expect(userService.getUser).toHaveBeenCalledWith('1');
    expect(userService.getUsers).not.toHaveBeenCalled();
    expect(component.user).toEqual(cachedUser);
  });

  it('loads user from API list when cache is empty', async () => {
    const loadedUser = userFixture();
    await setup('1', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(undefined),
        getUsers: vi.fn().mockReturnValue(of([loadedUser]))
      }
    });

    expect(userService.getUsers).toHaveBeenCalledTimes(1);
    expect(component.user).toEqual(loadedUser);
  });

  it('redirects to list when user cannot be found', async () => {
    await setup('missing-user', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(undefined),
        getUsers: vi.fn().mockReturnValue(of([]))
      }
    });

    expect(router.navigateByUrl).toHaveBeenCalledWith('/users');
  });

  it('navigates to edit and back routes', async () => {
    await setup('1', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(userFixture())
      }
    });

    component.onEdit();
    component.onBack();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/users/form/1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/users');
  });

  async function setup(
    userId: string,
    options?: {
      userServiceOverrides?: Partial<typeof userService>;
    }
  ): Promise<void> {
    userService = {
      getUsers: vi.fn().mockReturnValue(of([])),
      getUser: vi.fn().mockReturnValue(undefined),
      ...(options?.userServiceOverrides ?? {})
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [UserDetailComponent],
      providers: [
        { provide: UserService, useValue: userService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: vi.fn().mockReturnValue(userId)
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UserDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function userFixture(): User {
    return {
      id: '1',
      username: 'operator',
      firstName: 'Warehouse',
      lastName: 'Operator',
      email: 'operator@example.com',
      role: 'Operator',
      status: true,
      createdAt: new Date('2026-06-22T08:00:00Z')
    };
  }
});
