#define IOCPARM_MASK    0x7f            /* parameters must be < 128 bytes */
#define IOC_IN          0x80000000      /* copy in parameters */
#define _IOW(x,y,t)     (IOC_IN|(((long)sizeof(t)&IOCPARM_MASK)<<16)|((x)<<8)|(y))
#define FIONBIO     _IOW('f', 126, u_long)
typedef unsigned long   u_long;

int main(void) {
	printf("%x\n", FIONBIO);
}
