
import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";

import { authApi } from "../services/api/auth";
import { tokenStorage } from "../services/storage/token";

import { usersApi } from "../services/api/users";
import { ApiError } from "../services/api/client";

import type {
  User,
  AuthResponse,
  RegisterRequest,
   PreferredLanguage,
} from "../types/auth";

interface AuthContextType {
  user: User | null;
  token: string | null;
  loading: boolean;

  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  updatePreferredLanguage: (
    language: PreferredLanguage
  ) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(
  null
);

export function AuthProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [session, setSession] =
    useState<AuthResponse | null>(null);

  const [loading, setLoading] = useState(true);

  // 1. Restore authentication when opening the app.
  useEffect(() => {
    let mounted = true;

    async function restoreAuthentication() {
      try {
        const saved = await tokenStorage.get();

        if (saved) {
          const expiresAt = Date.parse(saved.expiresAt);

          if (
            saved.token &&
            saved.userId > 0 &&
            Number.isFinite(expiresAt) &&
            expiresAt > Date.now()
          ) {

            let restored = saved;

            try {
              const profile = await usersApi.getProfile(
                saved.token,
                saved.userId
              );
            
              restored = {
                ...saved,
                name: profile.name,
                email: profile.email,
                preferredLanguage: profile.preferredLanguage,
              };
            } catch (err) {
              if (
                err instanceof ApiError &&
                (err.status === 401 || err.status === 403)
              ) {
                await tokenStorage.remove();
                return;
              }
            
              console.warn("Could not refresh the saved profile.");
            }

            if (mounted) {
              setSession(restored);
            }
          } else {
            await tokenStorage.remove();
          }
        }
      } catch (error) {
        console.warn("Could not restore authentication.");
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    }

    restoreAuthentication();

    return () => {
      mounted = false;
    };
  }, []);

  // 2. Save the response returned by login/register.
  async function saveAuthentication(response: AuthResponse) {
    if (
      !response?.token ||
      !response?.userId ||
      !Number.isFinite(Date.parse(response.expiresAt)) ||
      Date.parse(response.expiresAt) <= Date.now()
    ) {
      throw new Error("Invalid authentication response.");
    }
    //fallback to make sure i am using the lATEST LANGUAGAGE UPDATE
    if (
       response.preferredLanguage !== "en" &&
       response.preferredLanguage !== "fr"
    )  {
       const profile = await usersApi.getProfile(
         response.token,
         response.userId
       );
      
       response = {
         ...response,
         preferredLanguage: profile.preferredLanguage,
       };
    } 
    await tokenStorage.save(response);
    setSession(response);
  }

  // 3. Login.
  async function login(email: string, password: string) {
    const response = await authApi.login({
      email,
      password,
    });

    await saveAuthentication(response);
  }

  // 4. Register. Backend also returns an AuthResponse.
  async function register(data: RegisterRequest) {
    const response = await authApi.register(data);

    await saveAuthentication(response);
  }

  // 5. Logout.
  async function logout() {
    await tokenStorage.remove();
    setSession(null);
  }

  // 6. Share only the user fields with the UI.
  const user: User | null = session
    ? {
        userId: session.userId,
        name: session.name,
        email: session.email,
        preferredLanguage:
         session.preferredLanguage === "fr" ? "fr" : "en",
      }
    : null;

  // 7 ; updatePreferred language:
    async function updatePreferredLanguage(
      language: PreferredLanguage
    ) {
      if (!session) {
        throw new Error("You must be logged in.");
      }
    
      const response =
        await usersApi.updatePreferredLanguage(
          session.token,
          language
        );
      
      const updatedSession: AuthResponse = {
        ...session,
        preferredLanguage: response.preferredLanguage,
      };
    
      await tokenStorage.save(updatedSession);
    
      setSession(updatedSession);
    }
  return (
    <AuthContext.Provider
      value={{
        user,
        token: session?.token ?? null,
        loading,
        
        login,
        register,
        logout,
        updatePreferredLanguage,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

// Custom hook for accessing authentication anywhere.
export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error(
      "useAuth must be used inside AuthProvider"
    );
  }

  return context;
}
