import { getServerSession } from "next-auth";
import { authOptions } from "../api/auth/[...nextauth]/route";

export async function getSession() {
  return await getServerSession(authOptions);
}

export async function getCurrentUser() {
  try {
    const session = getSession();
    console.log({ session });
    if (!session) return null;
  } catch (error) {}
}
