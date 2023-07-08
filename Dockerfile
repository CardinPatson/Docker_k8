FROM node:16-alpine as builder

WORKDIR "/app"
COPY package.json .
RUN npm install
COPY . .
RUN npm run build
# /app/build  contient les fichiers necessaires à la production

FROM nginx
COPY --from=builder /app/build /usr/share/nginx/html
#docker run -p 8080:80 <image-id>
