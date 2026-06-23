import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { CreateUser } from '../users/model/create-user';
import { User } from '../users/model/user';
import { UserService } from './user-service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  const users: User[] = [
    {
      id: '1',
      username: 'operator',
      firstName: 'Warehouse',
      lastName: 'Operator',
      email: 'operator@example.com',
      role: 'Operator',
      status: true,
      createdAt: new Date('2026-06-22T08:00:00Z')
    }
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets users from IDP API and stores them in local cache', () => {
    service.getUsers().subscribe(result => {
      expect(result).toEqual(users);
    });

    const req = httpMock.expectOne(`${environment.idpUrl}/api/users`);

    expect(req.request.method).toBe('GET');
    req.flush(users);
    expect(service.users).toEqual(users);
  });

  it('adds user to local cache with the next numeric id', () => {
    service.users = users;

    const createdUser = service.addUser(createUserPayload());

    expect(createdUser).toEqual(expect.objectContaining({
      id: '2',
      username: 'manager',
      email: 'manager@example.com'
    }));
    expect(createdUser.createdAt).toEqual(expect.any(Date));
    expect(service.users).toContain(createdUser);
  });

  it('starts local user ids from zero when cache is empty', () => {
    service.users = [];

    const createdUser = service.addUser(createUserPayload());

    expect(createdUser.id).toBe('0');
  });

  it('returns user from local cache by id', () => {
    service.users = users;

    expect(service.getUser('1')).toEqual(users[0]);
    expect(service.getUser('missing-user')).toBeUndefined();
  });

  function createUserPayload(): CreateUser {
    return {
      username: 'manager',
      firstName: 'Warehouse',
      lastName: 'Manager',
      email: 'manager@example.com',
      role: 'Manager',
      status: true
    };
  }
});
