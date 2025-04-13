# Build stage
FROM node:20-alpine AS build

# Set working directory
WORKDIR /app

# Copy package files
COPY package.json ./
COPY package-lock.json ./

# Install dependencies
RUN npm ci

# Copy the rest of the application
COPY . .

# Build the Next.js application
RUN npm run build

# Production stage
FROM node:20-alpine AS production

# Set working directory
WORKDIR /app

# Copy package files
COPY package.json ./
COPY package-lock.json ./

# Install only production dependencies
RUN npm ci --only=production

# Copy built assets from build stage
COPY --from=build /app/.next ./.next
COPY --from=build /app/public ./public

# Expose port 3000
EXPOSE 3000

# Start the Next.js application
CMD ["npm", "start"]