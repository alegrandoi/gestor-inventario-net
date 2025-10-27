/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  experimental: {
    typedRoutes: true,
    externalDir: true
  },
  output: 'standalone'
};

export default nextConfig;
