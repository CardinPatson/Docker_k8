## Schema de production

1. Création de fichier de config pour chaque service et déploiement
2. Test local sur minikube (cluster local)
3. Workflow github pour build les images et les déployer
4. Déploiement de l'application sur un cloud provider

## Node Port vs Cluster IP

## POstgres PVC (Persitent Volume Claim)

Le pod postgres va enregistrer les données dans son système de fichier(du conteneur) mais lorsque le pod sera détruit ou crash, les données seront perdus (dans le conteneur postgres)

Au lieu de cela le conteneur postgres n'enregistrera pas les données dans son système de fichier mais dans un volume qui est sur la machine hôte 

### Kubernetes volumes

Volume - Object qui permet au conteneur de stocker des données au niveau des pod 

### Volume vs Persistent volume

Simple Volume est attaché au pod et quand le pod va crashé le volume disparaitra aussie

Volume Persistant n'est pas attaché au pod et quand le pod crash, le volume restera intact 

### Persistent volume vs Persistent volumes claim

Persitent volume claim est le stockage que notre pod à besoin pour enregistrer les données (ex: stockage d'un ordinateur)

Volume persistant statique : les volumes qui sont déjà disponible pour le PVC
VOlume persistant dynamique: les volumes qui sont crée à la demande lorsque tu fais le PVC

Le PVC est donc une config qui fait la requête pour une certaine quantité de stockage 

### PVC config file

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: database-persistent-volume-claima 
spec:
  resources:
    requests:
      storage: 2Gi
  # volumeMode: Filesystem
  accessModes:
    - ReadWriteOnce
```
3 Types du mode d'accès : ReadWriteOnce (Utilisé par un seul node), ReadOnlyMany (Plusieurs node lit en même temps) , ReadWriteMany (...)


### Ou k8s alloue le volume persistent 

PVC(I need 2Gi) ==> K8S ==> (Make slice (PV) of the hard drive )

`kubectl get storageclass` (ou k8s va créer les volumes )
`kubectl describe storageclass`

Sur un cloud provider

(1. J'ai besoin d'1Gi) ==> K8S ==> (Google cloud persistent disk) or (Azure File) or (Azure Disk) or (AWS Block Store )
Dire à K8s ou il doit trouver le provider pour faire la persistance 

Pour cela on passe la propriété  **(storageClassName)** avec le nom du provider lorsqu'on configure le PVC 

### Associer le PVC au POD

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  # this 
  name: postgres-deployment
spec:
  replicas: 1
  selector:
    matchLabels:
      component: postgres
  template:
    metadata:
      labels:
        component: postgres
    spec:
      # Rajouter cette section de volume 
      volumes:
        - name: postgres-storage
          persistentVolumeClaim:
            claimName: database-persistent-volume-claim
      containers:
        - name: postgres
          image: postgres
          ports:
            - containerPort: 5432
          volumeMounts:
            - name: postgres-storage
              # Le répertoire ou postgres stocker les données sur le disque dur 
              mountPath: /var/lib/postgresql/data
```

`kubectl get pv` pour voir les volumes qui ont été crée
`kubectl get pvc` qui est une indication pour les besoins en volume

### Connexion du worker a redis

Pour se connecter d'un pod à un autre on passe **le nom du clusterIP** du pod auquel on souhaite se connecter

ex: 

(Worker Dep) ---> (redis-cluster-ip-service) ---> (ClusterIp Service)(Redis Dep)

Exemple dans le fichier Worker Dep
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: worker-deployment
spec:
  replicas: 1
  selector:
    matchLabels:
      component: worker
  template:
    metadata:
      labels:
        component: worker
    spec:
      containers:
        - name: worker
          image: stephengrider/multi-worker
          # Env to connect to redis pod
          env:
            - name: REDIS_HOST
              value: redis-cluster-ip-service
            - name: REDIS_PORT
              value: "6379"
          resources:
            limits:
              memory: "128Mi"
              cpu: "500m"
```

ex : 

(Pod) ===> (Service name) ===>  (ClusterIp Service)(Other Pods)

(Server Dep) ---> (redis-cluster-ip-service) and (postgres-cluster-ip-service) ---> (...)

### Secrets pour passer le mot de passe postgres 

Permet de stocker des informations de façon sécurisé dans le cluster comme le mot de passe de base de données 

Dans un environnement de production, les secrets on besoin d'être crée manuellement 

Commande

`kubectl create secret generic <secret-name> --from-literal key=value`

ex : `kubectl create secret generic pgpassword --from-literal PGPASSWORD=myPassword`

generic == type de secret (autre: docker-registry, tls(https setup: tls key) )
--from-literal == pour ajouter les informations du secret dans la commande

le secret-name va être référencé dans les autres pods
key=value ==> PGPASSWORD=postgres1234

Pour rajouter le secret parmi les variables d'environnement 
```yaml
          env:
            - name: PGPASSWORD 
              valueFrom:
                secretKeyRef:
                  name: pgpassword # secret-name
                  key: PGPASSWORD # key of the secret
```

## Traffic avec l'ingress controller

Le service d'ingress est la porte d'entrée. Il expose certain service au monde externe 

Parmis les type de service on a : (ClusterIp, NodePort, LoadBalancer, Ingress)
- LoadBalancer : Legacy way of getting network traffic into a cluster (Facon général d'avoir du traffic internet dans le cluster )
- ClusterIp : Expose les pods à d'autre pods dans le cluster

Le loadbalancer donne accès à seulement un seul pod (voir image) depuis le monde extérieur et quand on setup un loadbalancer , k8s va setup dans ton provider un load balancer pour envoyer le traffic dans ton pod 

### NGINX Ingress

le service s'appelle : ingress-nginx disponible sur https://github.com/kubernetes/ingress-nginx

autre service : kubernetes-ingress disponible sur https://github.com/nginxinc/kubernetes-ingress

La mise en place de l'ingress dépend de l'environnement (local, azure, aws, GC )

- Ingress pour Local et GC

- Derrière la scène

Le controller ingress : Vérifie l'état actuelle puis l'état désiré fait une comparaison et met à jour (voir image ingress-traffic-view1) les pod en rajoutant nginx pour le routage 

On commence par avoir une config ingress --> qui va crée un controller ingress --> qui va créer quelque chose qui permet d'accepter (de router) le traffic externe vers les services correspondant 

Ingress Config --> Object avec des règles de configuration qui définit comment le traffic doit être routé
Ingress COntroller --> Observe les changements de l'ingress et met à jour ce qui gère le traffic

### Ingress sur GC

1. Fair un fichier ingress config qui sera dans un déployment qui pourra être atteint via un load balancer service(Google Cloud load balancer)

On mettra en place un deployment necessaire pour du health check qui vérifiera si le cluster fonctionne de façon correcte 

Pour installer ingress nginx == https://kubernetes.github.io/ingress-nginx/deploy/#quick-start

Pour le déploiement sur azure == https://kubernetes.github.io/ingress-nginx/deploy/#azure

installer choco = Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))


la commande
`helm upgrade --install ingress-nginx ingress-nginx --repo https://kubernetes.github.io/ingress-nginx --namespace ingress-nginx --create-namespace`

## Déploiment sur le cloud


### sur la console de ton cluster

- gcloud config set project <project-id>

- gcloud config set compute/zone <cluster-zone>

- gcloud container clusters get-credentials <cluster-name >

### pour définir les variables d'environnment

- kubectl create secret generic pgpassword --from-litteral <KEY>=<VALUE>

dans la console de google cloud pour installer le controlleur ingress 

```bash
curl -fsSL -o get_helm.sh https://raw.githubusercontent.com/helm/helm/master/scripts/get-helm-3
chmod 700 get_helm.sh
./get_helm.sh

helm upgrade --install ingress-nginx ingress-nginx \
  --repo https://kubernetes.github.io/ingress-nginx \
  --namespace ingress-nginx --create-namespace

kubectl get service ingress-nginx-controller --namespace=ingress-nginx
```
--- 
Venant de la documentation : https://kubernetes.github.io/ingress-nginx/deploy/#gce-gke
```bash
kubectl create clusterrolebinding cluster-admin-binding \
  --clusterrole cluster-admin \
  --user $(gcloud config get-value account)

kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml
```
Terminology
- User Account
- Service Account
- CLusterRoleBinding : Authorize an account to do a set of actions accross the cluster
- RoleBinding:  to a set of actions in a single namespace 

### Sécurité RBAC de kubernetes (Optionel) : Dans HELMV3 l'utilisation de tiller est optionnel

On crée un nouveau compte de service nommé tiller

```bash
kubectl create service account --namespace kube-system tiller
```

On crée un nouveau clusterrolebinding avec le role cluster-admin et on l'assigne au compte de service tiller

```bash
kubectl create clusterrolebinding tiller-cluster-rule --clusterrole=cluster-admin --serviceaccount=kube-system:tiller
```

Avant de créer le controller ingress nginx, initialiser helm avec le compte de service tiller

```bash
helm init -- service-account tiller --upgrade
```

-- Set up maintenant le controlleur ingress nginx a l'aide de helm 

Dans la partie service de réseau tu peux voir le load balancer qui se trouve pour pouvoir atteindre l'ingress 

Le traffic arrive d'abord sur le load balancer service qui lui va toucher le controlleur ingress nginx