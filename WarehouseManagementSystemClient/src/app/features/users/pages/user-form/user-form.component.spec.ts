import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { UserService } from '../../../services/user-service';
import { User } from '../../model/user';
import { UserFormComponent } from './user-form.component';

describe('UserFormComponent', () => {
  let component: UserFormComponent;
  let fixture: ComponentFixture<UserFormComponent>;
  let userService: {
    getUsers: ReturnType<typeof vi.fn>;
    getUser: ReturnType<typeof vi.fn>;
    addUser: ReturnType<typeof vi.fn>;
  };
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  it('builds a form with required user fields and email validation', async () => {
    await setup();

    component.userForm.patchValue({
      username: '',
      firstName: '',
      lastName: '',
      email: 'not-an-email',
      role: '',
      status: ''
    });

    expect(component.userForm.valid).toBe(false);
    expect(component.userForm.get('username')?.hasError('required')).toBe(true);
    expect(component.userForm.get('firstName')?.hasError('required')).toBe(true);
    expect(component.userForm.get('lastName')?.hasError('required')).toBe(true);
    expect(component.userForm.get('email')?.hasError('email')).toBe(true);
    expect(component.userForm.get('role')?.hasError('required')).toBe(true);
    expect(component.userForm.get('status')?.hasError('required')).toBe(true);
  });

  it('creates a local user and navigates to created detail', async () => {
    await setup(null, {
      userServiceOverrides: {
        addUser: vi.fn().mockReturnValue({ id: '2', ...validUserPayload(), createdAt: new Date('2026-06-22T08:00:00Z') })
      }
    });

    fillValidForm();
    component.onSave();

    expect(userService.addUser).toHaveBeenCalledWith({
      id: '',
      ...validUserPayload()
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/users/detail/2');
  });

  it('lets a user fill and submit the create form from the rendered UI', async () => {
    await setup(null, {
      userServiceOverrides: {
        addUser: vi.fn().mockReturnValue({
          id: '2',
          username: 'operator',
          firstName: 'Warehouse',
          lastName: 'Operator',
          email: 'operator@example.com',
          role: 'Operator',
          status: 'true',
          createdAt: new Date('2026-06-22T08:00:00Z')
        })
      }
    });

    setInputValue('app-input-field[formcontrolname="email"] input', 'operator@example.com');
    setInputValue('app-input-field[formcontrolname="username"] input', 'operator');
    setInputValue('app-input-field[formcontrolname="firstName"] input', 'Warehouse');
    setInputValue('app-input-field[formcontrolname="lastName"] input', 'Operator');
    setInputValue('app-input-field[formcontrolname="role"] input', 'Operator');
    setInputValue('app-input-field[formcontrolname="status"] input', 'true');
    fixture.detectChanges();

    buttonByText('Save').click();
    await fixture.whenStable();

    expect(userService.addUser).toHaveBeenCalledWith({
      id: '',
      username: 'operator',
      firstName: 'Warehouse',
      lastName: 'Operator',
      email: 'operator@example.com',
      role: 'Operator',
      status: 'true'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/users/detail/2');
  });

  it('keeps save disabled in the rendered UI when email is invalid', async () => {
    await setup();

    setInputValue('app-input-field[formcontrolname="email"] input', 'not-an-email');
    setInputValue('app-input-field[formcontrolname="username"] input', 'operator');
    setInputValue('app-input-field[formcontrolname="firstName"] input', 'Warehouse');
    setInputValue('app-input-field[formcontrolname="lastName"] input', 'Operator');
    setInputValue('app-input-field[formcontrolname="role"] input', 'Operator');
    setInputValue('app-input-field[formcontrolname="status"] input', 'true');
    fixture.detectChanges();

    const saveButton = buttonByText('Save');
    expect(saveButton.disabled).toBe(true);

    saveButton.click();

    expect(userService.addUser).not.toHaveBeenCalled();
  });

  it('loads edit form from local user cache without calling users endpoint', async () => {
    const cachedUser = userFixture();
    await setup('1', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(cachedUser)
      }
    });

    expect(userService.getUser).toHaveBeenCalledWith('1');
    expect(userService.getUsers).not.toHaveBeenCalled();
    expect(component.userForm.value).toEqual(expect.objectContaining({
      id: '1',
      username: cachedUser.username,
      firstName: cachedUser.firstName,
      lastName: cachedUser.lastName,
      email: cachedUser.email,
      role: cachedUser.role,
      status: cachedUser.status
    }));
  });

  it('loads edit form from API list when user is not cached', async () => {
    await setup('1', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(undefined),
        getUsers: vi.fn().mockReturnValue(of([userFixture()]))
      }
    });

    expect(userService.getUsers).toHaveBeenCalledTimes(1);
    expect(component.userForm.get('username')?.value).toBe('operator');
  });

  it('redirects to list when edited user cannot be found', async () => {
    await setup('missing-user', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(undefined),
        getUsers: vi.fn().mockReturnValue(of([]))
      }
    });

    expect(router.navigateByUrl).toHaveBeenCalledWith('/users');
  });

  it('navigates to current user detail in edit mode without creating a new local user', async () => {
    await setup('1', {
      userServiceOverrides: {
        getUser: vi.fn().mockReturnValue(userFixture())
      }
    });

    component.userForm.patchValue({ firstName: 'Updated' });
    component.onSave();

    expect(userService.addUser).not.toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/users/detail/1');
  });

  it('navigates back to users list', async () => {
    await setup();

    component.onBack();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/users');
  });

  async function setup(
    userId: string | null = null,
    options?: {
      userServiceOverrides?: Partial<typeof userService>;
    }
  ): Promise<void> {
    userService = {
      getUsers: vi.fn().mockReturnValue(of([])),
      getUser: vi.fn().mockReturnValue(undefined),
      addUser: vi.fn(),
      ...(options?.userServiceOverrides ?? {})
    };
    router = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [UserFormComponent],
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

    fixture = TestBed.createComponent(UserFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function fillValidForm(): void {
    component.userForm.patchValue(validUserPayload());
  }

  function validUserPayload() {
    return {
      username: 'operator',
      firstName: 'Warehouse',
      lastName: 'Operator',
      email: 'operator@example.com',
      role: 'Operator',
      status: true
    };
  }

  function userFixture(): User {
    return {
      id: '1',
      ...validUserPayload(),
      createdAt: new Date('2026-06-22T08:00:00Z')
    };
  }

  function setInputValue(selector: string, value: string): void {
    const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function buttonByText(text: string): HTMLButtonElement {
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const button = buttons.find(candidate => candidate.textContent?.trim() === text);
    if (!button) {
      throw new Error(`Button "${text}" was not found.`);
    }
    return button;
  }
});
