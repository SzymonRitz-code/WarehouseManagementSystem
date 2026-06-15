import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreateUser } from '../users/model/create-user';
import { User } from '../users/model/user';
import { environment } from '../../environments/environment';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class UserService {

  private readonly apiUrl = `${environment.idpUrl}/api/users`;
  users: User[] = [];

  constructor(private http: HttpClient) { }

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl).pipe(
      tap(users => this.users = users)
    );
  }

  addUser(user: CreateUser) {
    const newId = this.users.length > 0
      ? Math.max(...this.users.map(p => Number(p.id))) + 1
      : 0;
    const userToAdd: User = {
      ...user,
      id: newId.toString(),
      createdAt: new Date()
    }
    this.users.push(userToAdd);
    return userToAdd;
  }
  getUser(id: string) {
    return this.users.find(u => u.id === id)
  }

}
