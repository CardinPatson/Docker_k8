docker build -t cardin21/multi-client -f ./client/Dockerfile ./client
docker build -t cardin21/multi-server -f ./server/Dockerfile ./server
docker build -t cardin21/multi-worker -f ./server/Dockerfile ./worker 
docker push cardin21/multi-client
docker push cardin21/multi-server
docker push cardin21/multi-worker
kubectl apply -f k8s
kubectl set image deployments/server-deployment server=cardin21/multi-server
#git rev-parse HEAD