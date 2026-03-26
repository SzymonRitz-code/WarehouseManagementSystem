import { CreateUser } from "./create-user"

export interface User extends CreateUser {
    id: string,
    createdAt: Date,
    updatedAt?: Date
}

